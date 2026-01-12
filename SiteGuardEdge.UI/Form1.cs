using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum; // Add this line
using SiteGuardEdge.Infrastructure.Video;
using SiteGuardEdge.Infrastructure.AI;
using SiteGuardEdge.Infrastructure.Data.Repositories;
using SiteGuardEdge.Domain.Interfaces;
using SiteGuardEdge.Domain.Entities;
using SiteGuardEdge.Application; // Add this line
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using SiteGuardEdge.Infrastructure.Data;

namespace SiteGuardEdge.UI;

public partial class Form1 : Form
{
    private IVideoSource _videoSource;
    private OnnxPpeDetector _ppeDetector;
    private IDetectionEventRepository _detectionEventRepository;
    private IConfigurationService _configurationService; // Add this line
    private bool _isCapturing = false;
    private DateTime _lastDetectionTime = DateTime.MinValue;
    private readonly TimeSpan _detectionInterval = TimeSpan.FromSeconds(5);

    public Form1(IDetectionEventRepository detectionEventRepository, IConfigurationService configurationService)
    {
        InitializeComponent();
        _detectionEventRepository = detectionEventRepository;
        _configurationService = configurationService;
        this.Text = "SiteGuard PPE";
        InitializePpeDetector();
        InitializeDatabase(); // This will now use the injected repository
    }

    private void InitializePpeDetector()
    {
        string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "yolov8n.onnx");
        bool forceCpu = bool.Parse(ConfigurationManager.AppSettings["ForceCpu"] ?? "false");
        bool forceGpu = bool.Parse(ConfigurationManager.AppSettings["ForceGpu"] ?? "false");
        _ppeDetector = new OnnxPpeDetector(modelPath, forceCpu: forceCpu, forceGpu: forceGpu);
    }

    private async void InitializeDatabase()
    {
        var builder = new DbContextOptionsBuilder<SiteGuardEdgeDbContext>();
        builder.UseSqlServer(ConfigurationManager.ConnectionStrings["SiteGuardEdgeDB"].ConnectionString);
        var dbContext = new SiteGuardEdgeDbContext(builder.Options);
        await dbContext.Database.MigrateAsync();
        // _detectionEventRepository is now injected
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        PopulateWebcamDevices();
    }

    private void PopulateWebcamDevices()
    {
        var devices = EmguCvVideoSource.GetAvailableDevices();
        cbWebcamDevices.DataSource = devices;
        if (devices.Any())
        {
            cbWebcamDevices.SelectedIndex = 0;
        }
    }

    private async void btnStartStop_Click(object sender, EventArgs e)
    {
        if (!_isCapturing)
        {
            // Start capture
            if (cbWebcamDevices.SelectedItem is VideoDevice device)
            {
                try
                {
                    _videoSource = new EmguCvVideoSource(device.Index);
                    _videoSource.OnFrameReady += VideoSource_OnFrameReady;
                    await _videoSource.StartAsync();
                    _isCapturing = true;
                    btnStartStop.Text = "Stop Feed";
                    btnBrowseVideo.Enabled = false;
                    cbWebcamDevices.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error starting webcam: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a webcam device or browse a video file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            // Stop capture
            if (_videoSource != null)
            {
                await _videoSource.StopAsync();
                _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
                _videoSource.Dispose();
                _videoSource = null;
            }
            _isCapturing = false;
            btnStartStop.Text = "Start Feed";
            btnBrowseVideo.Enabled = true;
            cbWebcamDevices.Enabled = true;
            pbVideoFeed.Image = null; // Clear the picture box
            lblFrameTimestamp.Text = string.Empty;
        }
    }

    private async void btnBrowseVideo_Click(object sender, EventArgs e)
    {
        if (openFileDialogStill.ShowDialog() == DialogResult.OK)
        {
            if (_isCapturing)
            {
                await _videoSource.StopAsync();
                _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
                _videoSource.Dispose();
                _videoSource = null;
                _isCapturing = false;
            }

            try
            {
                _videoSource = new EmguCvVideoSource(openFileDialogStill.FileName);
                _videoSource.OnFrameReady += VideoSource_OnFrameReady;
                await _videoSource.StartAsync();
                _isCapturing = true;
                btnStartStop.Text = "Stop Feed";
                btnBrowseVideo.Enabled = false;
                cbWebcamDevices.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading video file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStartStop.Text = "Start Feed";
                btnBrowseVideo.Enabled = true;
                cbWebcamDevices.Enabled = true;
            }
        }
    }

    private async void VideoSource_OnFrameReady(object sender, (Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp) frameData)
    {
        if (pbVideoFeed.InvokeRequired)
        {
            pbVideoFeed.Invoke(new Action(async () => await ProcessFrame(frameData.Image, frameData.FrameNumber, frameData.TotalFrames, frameData.Timestamp)));
        }
        else
        {
            await ProcessFrame(frameData.Image, frameData.FrameNumber, frameData.TotalFrames, frameData.Timestamp);
        }
    }

    private async Task ProcessFrame(Image<Bgr, byte> image, int frameNumber, int totalFrames, TimeSpan? timestamp)
    {
        // Update timestamp for recorded videos
        if (_videoSource.IsFile && timestamp.HasValue)
        {
            lblFrameTimestamp.Text = $"Frame: {frameNumber}/{totalFrames} | Time: {timestamp.Value:hh\\:mm\\:ss\\.ff}";
        }
        else
        {
            lblFrameTimestamp.Text = string.Empty;
        }

        // Perform detection every 5 seconds for live feeds, or for every sampled frame in recorded videos
        bool shouldPerformDetection = false;
        if (!_videoSource.IsFile) // Live webcam
        {
            if (DateTime.Now - _lastDetectionTime >= _detectionInterval)
            {
                shouldPerformDetection = true;
                _lastDetectionTime = DateTime.Now;
            }
        }
        else // Recorded video
        {
            // For recorded videos, we rely on the EmguCvVideoSource to provide frames at appropriate intervals
            // The OnFrameReady event will be triggered for frames corresponding to ~5-second intervals
            shouldPerformDetection = true;
        }

        if (shouldPerformDetection)
        {
            var ppeResults = await _ppeDetector.DetectPpeAsync(image.Mat);

            // Draw bounding boxes and labels
            foreach (var result in ppeResults)
            {
                // Draw person bounding box
                Color personBoxColor = result.IsCompliant ? Color.Green : Color.Red;
                CvInvoke.Rectangle(image, result.Person.BoundingBox, new Bgr(personBoxColor).MCvScalar, 2);

                // Draw PPE items and labels
                foreach (var ppe in result.DetectedPpe)
                {
                    CvInvoke.Rectangle(image, ppe.BoundingBox, new Bgr(Color.Blue).MCvScalar, 1);
                    CvInvoke.PutText(image, ppe.ClassLabel, new Point(ppe.BoundingBox.X, ppe.BoundingBox.Y - 5),
                                     Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new Bgr(Color.Blue).MCvScalar);
                }

                // Indicate missing PPE
                var missingPpe = result.PpePresence.Where(kv => !kv.Value).Select(kv => kv.Key.ToString()).ToList();
                if (missingPpe.Any())
                {
                    string missingText = "Missing: " + string.Join(", ", missingPpe);
                    CvInvoke.PutText(image, missingText, new Point(result.Person.BoundingBox.X, result.Person.BoundingBox.Y + result.Person.BoundingBox.Height + 15),
                                     Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.6, new Bgr(Color.Red).MCvScalar, 2);
                }

                // Capture and store snapshot for non-compliant events
                string snapshotPath = "N/A";
                if (!result.IsCompliant)
                {
                    string snapshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SiteGuardEdge_Snapshots");
                    if (!Directory.Exists(snapshotsDir))
                    {
                        Directory.CreateDirectory(snapshotsDir);
                    }
                    string fileName = $"SiteGuardEdge_{DateTime.Now:yyyyMMdd_HHmmss}_{(_videoSource.IsFile ? Path.GetFileNameWithoutExtension(_videoSource.FilePath) : "Webcam")}_NonCompliant_{Guid.NewGuid()}.png";
                    snapshotPath = Path.Combine(snapshotsDir, fileName);
                    image.Save(snapshotPath);
                }

                // Log detection event
                await _detectionEventRepository.AddDetectionEventAsync(new DetectionEvent
                {
                    Timestamp = DateTime.Now,
                    VideoSource = _videoSource.IsFile ? Path.GetFileName(_videoSource.FilePath) : "Webcam",
                    FrameTimestamp = timestamp,
                    PPE_Detected = string.Join(",", result.DetectedPpe.Select(p => p.ClassLabel)),
                    PPE_Missing = string.Join(",", missingPpe),
                    ComplianceStatus = result.IsCompliant ? "Compliant" : "Non-Compliant",
                    ConfidenceScore = result.Person.Confidence,
                    BoundingBoxCoordinates = $"{result.Person.BoundingBox.X},{result.Person.BoundingBox.Y},{result.Person.BoundingBox.Width},{result.Person.BoundingBox.Height}", // Simplified
                    SnapshotPath = snapshotPath
                });
            }
        }

        pbVideoFeed.Image = image.ToBitmap();
    }

    private async void btnStill_Click(object sender, EventArgs e)
    {
        // Stop any active video feed if running
        if (_isCapturing)
        {
            await _videoSource.StopAsync();
            _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
            _videoSource.Dispose();
            _videoSource = null;
            _isCapturing = false;
            btnStartStop.Text = "Start Feed";
            btnBrowseVideo.Enabled = true;
            cbWebcamDevices.Enabled = true;
            pbVideoFeed.Image = null; // Clear the picture box
            lblFrameTimestamp.Text = string.Empty;
        }

        // Set filter for image files and title for the dialog
        openFileDialogStill.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";
        openFileDialogStill.Title = "Select an Image File";

        if (openFileDialogStill.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // Load the image into Emgu.CV.Image<Bgr, byte>
                using (var image = new Image<Bgr, byte>(openFileDialogStill.FileName))
                {
                    // Perform PPE detection on the still image
                    var ppeResults = await _ppeDetector.DetectPpeAsync(image.Mat);

                    // Draw bounding boxes and labels on the image (reusing existing logic from ProcessFrame)
                    foreach (var result in ppeResults)
                    {
                        // Draw person bounding box
                        Color personBoxColor = result.IsCompliant ? Color.Green : Color.Red;
                        CvInvoke.Rectangle(image, result.Person.BoundingBox, new Bgr(personBoxColor).MCvScalar, 2);

                        // Draw PPE items and labels
                        foreach (var ppe in result.DetectedPpe)
                        {
                            CvInvoke.Rectangle(image, ppe.BoundingBox, new Bgr(Color.Blue).MCvScalar, 1);
                            CvInvoke.PutText(image, ppe.ClassLabel, new Point(ppe.BoundingBox.X, ppe.BoundingBox.Y - 5),
                                             Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new Bgr(Color.Blue).MCvScalar);
                        }

                        // Indicate missing PPE
                        var missingPpe = result.PpePresence.Where(kv => !kv.Value).Select(kv => kv.Key.ToString()).ToList();
                        if (missingPpe.Any())
                        {
                            string missingText = "Missing: " + string.Join(", ", missingPpe);
                            CvInvoke.PutText(image, missingText, new Point(result.Person.BoundingBox.X, result.Person.BoundingBox.Y + result.Person.BoundingBox.Height + 15),
                                             Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.6, new Bgr(Color.Red).MCvScalar, 2);
                        }

                        // Capture and store snapshot for non-compliant events
                        string snapshotPath = "N/A";
                        if (!result.IsCompliant)
                        {
                            string snapshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SiteGuardEdge_Snapshots");
                            if (!Directory.Exists(snapshotsDir))
                            {
                                Directory.CreateDirectory(snapshotsDir);
                            }
                            string fileName = $"SiteGuardEdge_{DateTime.Now:yyyyMMdd_HHmmss}_StillImage_NonCompliant_{Guid.NewGuid()}.png";
                            snapshotPath = Path.Combine(snapshotsDir, fileName);
                            image.Save(snapshotPath);
                        }

                        // Log detection event
                        await _detectionEventRepository.AddDetectionEventAsync(new DetectionEvent
                        {
                            Timestamp = DateTime.Now,
                            VideoSource = "Still Image",
                            FrameTimestamp = null, // No frame timestamp for still images
                            PPE_Detected = string.Join(",", result.DetectedPpe.Select(p => p.ClassLabel)),
                            PPE_Missing = string.Join(",", missingPpe),
                            ComplianceStatus = result.IsCompliant ? "Compliant" : "Non-Compliant",
                            ConfidenceScore = result.Person.Confidence,
                            BoundingBoxCoordinates = $"{result.Person.BoundingBox.X},{result.Person.BoundingBox.Y},{result.Person.BoundingBox.Width},{result.Person.BoundingBox.Height}",
                            SnapshotPath = snapshotPath
                        });
                    }

                    // Display the processed image in pbVideoFeed
                    pbVideoFeed.Image = image.ToBitmap();
                    lblFrameTimestamp.Text = $"Still Image: {Path.GetFileName(openFileDialogStill.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading or processing image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Re-enable controls if an error occurs during still image processing
                btnStartStop.Enabled = true;
                btnBrowseVideo.Enabled = true;
                cbWebcamDevices.Enabled = true;
            }
        }
        else
        {
            // If dialog is cancelled, ensure controls are re-enabled if they were disabled
            if (!_isCapturing) // Only re-enable if not currently capturing video
            {
                btnStartStop.Enabled = true;
                btnBrowseVideo.Enabled = true;
                cbWebcamDevices.Enabled = true;
            }
        }
    }

    private void btnViewLogs_Click(object sender, EventArgs e)
    {
        var logViewerForm = new LogViewerForm(_detectionEventRepository);
        logViewerForm.Show();
    }

    private void btnSettings_Click(object sender, EventArgs e)
    {
        var settingsForm = new SettingsForm(_configurationService);
        settingsForm.ShowDialog(); // ShowDialog to block until settings are saved
    }

   
}
