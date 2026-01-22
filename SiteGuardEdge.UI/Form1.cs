using DirectShowLib;
using Emgu.CV;
using Emgu.CV.CvEnum; // Add this line
using Emgu.CV.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SiteGuardEdge.Application; // Add this line
using SiteGuardEdge.Domain.Entities;
using SiteGuardEdge.Domain.Interfaces;
using SiteGuardEdge.Infrastructure.AI;
using SiteGuardEdge.Infrastructure.Data;
using SiteGuardEdge.Infrastructure.Data.Repositories;
using SiteGuardEdge.Infrastructure.Video;
using System.Configuration;
using System.Diagnostics;
namespace SiteGuardEdge.UI;

public partial class Form1 : Form
{
    private IVideoSource _videoSource;
    private OnnxPpeDetector _ppeDetector;
    private OnnxPersonDetector _personDetector;
    private IDetectionEventRepository _detectionEventRepository;
    private IConfigurationService _configurationService; // Add this line
    private bool _isCapturing = false;
    private DateTime _lastDetectionTime = DateTime.MinValue;
    private readonly TimeSpan _detectionInterval = TimeSpan.FromSeconds(5);

    private VideoWriter _videoWriter;
    private string _outputFilePath;

    // Configuration Variables -- recording folder, etc.
    private string _recordingFolder;
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
        try
        { // Resolve model path
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "ppe_detection.onnx"); // s up from n
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"ONNX model not found at: {modelPath}");
            }
            // model path for person detector model
            string personModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "yolov8n_person.onnx");

            bool forceCpu = bool.TryParse(
                ConfigurationManager.AppSettings["ForceCpu"], out var cpuFlag) && cpuFlag;
            bool forceGpu = bool.TryParse(ConfigurationManager.AppSettings["ForceGpu"], out var gpuFlag) && gpuFlag;
            _ppeDetector = new OnnxPpeDetector(modelPath, forceCpu: forceCpu, forceGpu: forceGpu);
            Debug.WriteLine("[PPE] Detector initialized successfully");
            // Construct person detector
            _personDetector = new OnnxPersonDetector(modelPath);
            Debug.WriteLine("[Person] Detector initialized successfully");

        }
        catch (Exception ex)
        {
            string message = "Failed to initialize PPE detector:\n\n" + $"{ex.GetType().Name}: {ex.Message}";
            Debug.WriteLine("[PPE] ERROR: " + message);
            MessageBox.Show(message, "PPE Detector Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
        LoadApplicationSettings();
    }

    //private void PopulateWebcamDevices()
    //{
    //    var devices = EmguCvVideoSource.GetAvailableDevices();
    //    cbWebcamDevices.DataSource = devices;
    //    if (devices.Any())
    //    {
    //        cbWebcamDevices.SelectedIndex = 0;
    //    }
    //}


    private void PopulateWebcamDevices()
    {
        List<VideoDevice> devices = new List<VideoDevice>();

        // Get all video input devices from the OS
        DsDevice[] dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

        for (int i = 0; i < dsDevices.Length; i++)
        {
            devices.Add(new VideoDevice
            {
                Index = i,
                Name = dsDevices[i].Name
            });
        }

        // Bind to your ComboBox
        cbWebcamDevices.DataSource = devices;
        cbWebcamDevices.DisplayMember = "Name";

        if (devices.Any())
        {
            cbWebcamDevices.SelectedIndex = 0;
        }
    }

    //private async void btnStartStop_Click(object sender, EventArgs e)
    //    {
    //        if (!_isCapturing)
    //        {
    //            // Start capture
    //            if (cbWebcamDevices.SelectedItem is VideoDevice device)
    //            {
    //                try
    //                {
    //                    _videoSource = new EmguCvVideoSource(device.Index);
    //                    _videoSource.OnFrameReady += VideoSource_OnFrameReady;
    //                    await _videoSource.StartAsync();
    //                    _isCapturing = true;
    //                    btnStartStop.Text = "Stop Feed";
    //                    btnBrowseVideo.Enabled = false;
    //                    cbWebcamDevices.Enabled = false;
    //                }
    //                catch (Exception ex)
    //                {
    //                    MessageBox.Show($"Error starting webcam: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //                }
    //            }
    //            else
    //            {
    //                MessageBox.Show("Please select a webcam device or browse a video file.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //            }
    //        }
    //        else
    //        {
    //            // Stop capture
    //            if (_videoSource != null)
    //            {
    //                await _videoSource.StopAsync();
    //                _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
    //                _videoSource.Dispose();
    //                _videoSource = null;
    //            }
    //            _isCapturing = false;
    //            btnStartStop.Text = "Start Feed";
    //            btnBrowseVideo.Enabled = true;
    //            cbWebcamDevices.Enabled = true;
    //            pbVideoFeed.Image = null; // Clear the picture box
    //            lblFrameTimestamp.Text = string.Empty;
    //        }
    //    }

    private async void btnStartStop_Click(object sender, EventArgs e)
    {
        try
        {
            if (!_isCapturing)
            {
                if (!(cbWebcamDevices.SelectedItem is VideoDevice device)) return;

                _videoSource = new EmguCvVideoSource(device.Index);
                _videoSource.OnFrameReady += VideoSource_OnFrameReady;

                await _videoSource.StartAsync();
                _isCapturing = true;
                UpdateUIState(true);
            }
            else
            {
                await StopCaptureInternal();
                _isCapturing = false;
                UpdateUIState(false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Capture Error: {ex.Message}");
        }
    }


    //        // ARCHITECT NOTE: Initialize the Writer here

    //        // We fetch one frame or use default settings to get dimensions
    //        int width = 640;  // Better: get these from _videoSource if possible
    //            int height = 480;
    //            double fps = 30.0;
    //            _outputFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
    //            var fourcc = VideoWriter.Fourcc('M', 'P', '4', 'V');
    //            var frameSize = new Size(width, height);
    //            InitializeVideoWriter(frameSize);
    //            //// FourCC 'XVID' or 'H264' (may require openh264-2.x.x.dll in bin)
    //            _videoWriter = new VideoWriter(_outputFilePath, fourcc, fps, frameSize, true);

    //            if (!_videoWriter.IsOpened)
    //            {
    //                // This is where most failures happen
    //                var errorMsg = $"Failed to initialize VideoWriter!\n" +
    //                               $"Path: {_outputFilePath}\n" +
    //                               $"Size: {frameSize.Width}x{frameSize.Height}\n" +
    //                               $"FourCC: {fourcc}";
    //                MessageBox.Show(errorMsg, "Architect Diagnostic");
    //                return;
    //            }
    //            _isCapturing = true;
    //            btnStartStop.Text = "Stop Feed & Save";
    //            // ... (rest of your UI logic)
    //        }
    //        catch (Exception ex) { /* ... */ }
    //    }
    //}
    //else
    //{
    //    // STOP & SAVE LOGIC
    //    if (_videoSource != null)
    //    {
    //        await _videoSource.StopAsync();
    //        _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
    //        _videoSource.Dispose();
    //        _videoSource = null;
    //    }

    //    if (_videoWriter != null)
    //    {
    //        _videoWriter.Dispose(); // This finalizes the file header
    //        _videoWriter = null;
    //        MessageBox.Show($"Video saved to: {_outputFilePath}");
    //    }

    //_isCapturing = false;
    //btnStartStop.Text = "Start Feed";
    // ... (rest of your UI logic)
    //    }
    //}

    private void UpdateUIState(bool isCapturing)
    {
        // Ensure this runs on the UI thread
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() => UpdateUIState(isCapturing)));
            return;
        }

        btnStartStop.Text = isCapturing ? "Stop Feed & Save" : "Start Feed";
        btnStartStop.BackColor = isCapturing ? Color.Maroon : Color.Gainsboro;
        btnStartStop.ForeColor = isCapturing ? Color.White : Color.Black;

        // Disable selection and browsing while recording to prevent hardware conflicts
        cbWebcamDevices.Enabled = !isCapturing;
        btnBrowseVideo.Enabled = !isCapturing;

        if (!isCapturing)
        {
            pbVideoFeed.Image = null;
            lblFrameTimestamp.Text = "--:--:--";
        }
    }
    private async Task StopCaptureInternal()
    {
        if (_videoSource != null)
        {
            await _videoSource.StopAsync();
            _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
            _videoSource.Dispose();
            _videoSource = null;
        }

        if (_videoWriter != null)
        {
            _videoWriter.Dispose();
            _videoWriter = null;
            //  MessageBox.Show($"Video saved successfully: {_outputFilePath}");
            LogAndStatus($"Capture saved to {_outputFilePath}");

        }
    }
    //private void InitializeVideoWriter(Size frameSize)
    //{
    //    // 1. Ensure the directory exists
    //    string folder = Path.Combine(System.Windows.Forms.Application.StartupPath, "Recordings");
    //    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

    //    _outputFilePath = Path.Combine(folder, $"test_{DateTime.Now:yyyyMMdd_HHmmss}.avi");

    //    // 2. Use MJPG - it's the most likely to work without special DLLs
    //    int fourcc = VideoWriter.Fourcc('M', 'J', 'P', 'G');

    //    // 3. Match the EXACT size of the incoming frame
    //    _videoWriter = new VideoWriter(_outputFilePath, fourcc, 20.0, frameSize, true);

    //    if (_videoWriter.IsOpened)
    //    {
    //        Console.WriteLine($"SUCCESS: Writing to {_outputFilePath}");
    //    }
    //    else
    //    {
    //        Console.WriteLine("FAILURE: VideoWriter would not open.");
    //    }
    //}
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

    //private async void VideoSource_OnFrameReady(object sender, (Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp) frameData)
    //{
    //    if (pbVideoFeed.InvokeRequired)
    //    {
    //        pbVideoFeed.Invoke(new Action(async () => await ProcessFrame(frameData.Image, frameData.FrameNumber, frameData.TotalFrames, frameData.Timestamp)));
    //    }
    //    else
    //    {
    //        await ProcessFrame(frameData.Image, frameData.FrameNumber, frameData.TotalFrames, frameData.Timestamp);
    //    }
    //}

    private void InitializeVideoWriter(Size actualFrameSize)
    {
        string fileName = $"Rec_{DateTime.Now:yyyyMMdd_HHmmss}.avi";
        _outputFilePath = Path.Combine(_recordingFolder, fileName);

        //_outputFilePath = Path.Combine(folder, $"Rec_{DateTime.Now:yyyyMMdd_HHmmss}.avi");

        // MJPG is the Architect's choice for reliability on Windows
        int fourcc = VideoWriter.Fourcc('M', 'J', 'P', 'G');

        // Use the actual hardware FPS if your source provides it, otherwise 20-30
        _videoWriter = new VideoWriter(_outputFilePath, fourcc, 20.0, actualFrameSize, true);
        if (_videoWriter.IsOpened ) LogAndStatus($"Recording started: {fileName}");
        else
        {
            _isCapturing = false;
            LogAndStatus("Failed to open VideoWriter. Check codec/permissions.", true);
        }
    }
    private void VideoSource_OnFrameReady(object sender, (Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp) frameData)
    {
        // 1. Marshall to UI
        pbVideoFeed.BeginInvoke(new Action(() =>
        {
            pbVideoFeed.Image = frameData.Image.ToBitmap();
        }));

        // 2. Handle Recording
        if (_isCapturing)
        {
            // Lazy Init: If writer is null, create it using real frame metadata
            if (_videoWriter == null)
            {
                InitializeVideoWriter(frameData.Image.Size);
            }

            if (_videoWriter != null && _videoWriter.IsOpened)
            {
                _videoWriter.Write(frameData.Image.Mat);
            }
        }
    }

    private void UpdateUI((Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp) frameData)
    {
        // Convert Emgu Image to Bitmap for the WinForms PictureBox
        pbVideoFeed.Image = frameData.Image.ToBitmap();
        lblFrameTimestamp.Text = frameData.Timestamp?.ToString(@"hh\:mm\:ss") ?? string.Empty;
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
        await StopVideoFeedIfRunning();

        openFileDialogStill.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";
        openFileDialogStill.Title = "Select an Image File";

        if (openFileDialogStill.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var processedImage = await ProcessStillImage(openFileDialogStill.FileName);

            pbVideoFeed.Image = processedImage.ToBitmap();
            lblFrameTimestamp.Text = $"Still Image: {Path.GetFileName(openFileDialogStill.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading or processing image: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private async Task StopVideoFeedIfRunning()
    {
        if (!_isCapturing)
            return;

        await _videoSource.StopAsync();
        _videoSource.OnFrameReady -= VideoSource_OnFrameReady;
        _videoSource.Dispose();
        _videoSource = null;
        _isCapturing = false;

        btnStartStop.Text = "Start Feed";
        btnBrowseVideo.Enabled = true;
        cbWebcamDevices.Enabled = true;
        pbVideoFeed.Image = null;
        lblFrameTimestamp.Text = string.Empty;
    }

    private async Task<Image<Bgr, byte>> ProcessStillImage(string filePath)
    {
        var image = new Image<Bgr, byte>(filePath);

        // 1. Person detection (optional)
        //var persons = _personDetector?.DetectPersons(image.Mat) ?? new List<DetectedObject>();

        //if (_personDetector != null && !persons.Any())
        //{
        //    CvInvoke.PutText(image, "No person detected", new Point(10, 30),
        //        Emgu.CV.CvEnum.FontFace.HersheySimplex, 1.5,
        //        new Bgr(Color.Red).MCvScalar, 3);

        //    return image;
        //}
        //var persons = _personDetector.DetectPersons(image.Mat); 
        //if (!persons.Any()) 
        //{ 
        //    DrawNoPersonMessage(image); 
        //    return image; 
        //}

        // 2. PPE detection
        var ppeResults = await _ppeDetector.DetectPpeAsync(image.Mat);
        var result = ppeResults.FirstOrDefault();

        if (result == null)
        {
            CvInvoke.PutText(image, "No PPE detected", new Point(10, 30),
                Emgu.CV.CvEnum.FontFace.HersheySimplex, 1.5,
                new Bgr(Color.Red).MCvScalar, 3);

            return image;
        }

        // 3. Draw overlays
        DrawPpeOverlays(image, result);

        // 4. Save snapshot if needed
        string snapshotPath = SaveSnapshotIfNeeded(image, result);

        // 5. Log event
        await LogStillImageEvent(result, snapshotPath);

        return image;
    }
    private void DrawNoPersonMessage(Image<Bgr, byte> image)
    {
        CvInvoke.PutText(
            image,
            "No person detected",
            new Point(10, 30),
            Emgu.CV.CvEnum.FontFace.HersheySimplex,
            1.5,
            new Bgr(Color.Red).MCvScalar,
            3
        );
    }

    private void DrawPpeOverlays(Image<Bgr, byte> image, PpeComplianceResult result)
    {
        double scale = Math.Max(image.Width, image.Height) / 1000.0;
        scale = Math.Min(Math.Max(scale, 0.8), 3.0);
        int thickness = (int)Math.Max(2, scale * 2);

        foreach (var ppe in result.DetectedPpe)
        {
            CvInvoke.Rectangle(image, ppe.BoundingBox, new Bgr(Color.Blue).MCvScalar, 2);
            CvInvoke.PutText(image, ppe.ClassLabel,
                new Point(ppe.BoundingBox.X, ppe.BoundingBox.Y - 10),
                Emgu.CV.CvEnum.FontFace.HersheySimplex,
                scale,
                new Bgr(Color.Blue).MCvScalar,
                thickness);
        }

        var missing = result.PpePresence.Where(kv => !kv.Value).Select(kv => kv.Key.ToString());
        string text = result.IsCompliant ? "Compliant" : "Missing: " + string.Join(", ", missing);

        CvInvoke.PutText(image, text, new Point(10, 30),
            Emgu.CV.CvEnum.FontFace.HersheySimplex,
            1.2,
            result.IsCompliant ? new Bgr(Color.Green).MCvScalar : new Bgr(Color.Red).MCvScalar,
            3);
    }

    private string SaveSnapshotIfNeeded(Image<Bgr, byte> image, PpeComplianceResult result)
    {
        if (result.IsCompliant)
            return "N/A";

        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SiteGuardEdge_Snapshots");
        Directory.CreateDirectory(dir);

        string fileName = $"SiteGuardEdge_{DateTime.Now:yyyyMMdd_HHmmss}_StillImage_NonCompliant_{Guid.NewGuid()}.png";
        string path = Path.Combine(dir, fileName);

        image.Save(path);
        return path;
    }

    private async Task LogStillImageEvent(PpeComplianceResult result, string snapshotPath)
    {
        var missing = result.PpePresence.Where(kv => !kv.Value).Select(kv => kv.Key.ToString());

        await _detectionEventRepository.AddDetectionEventAsync(new DetectionEvent
        {
            Timestamp = DateTime.Now,
            VideoSource = "Still Image",
            FrameTimestamp = null,
            PPE_Detected = string.Join(",", result.DetectedPpe.Select(p => p.ClassLabel)),
            PPE_Missing = string.Join(",", missing),
            ComplianceStatus = result.IsCompliant ? "Compliant" : "Non-Compliant",
            ConfidenceScore = result.DetectedPpe.Any() ? result.DetectedPpe.Max(p => p.Confidence) : 0,
            BoundingBoxCoordinates = "N/A",
            SnapshotPath = snapshotPath
        });
    }

    private async void btnStill_Click_old(object sender, EventArgs e)
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

                    // PPE-only mode: ppeResults will contain exactly ONE result object
                    var result = ppeResults.FirstOrDefault();

                    if (result == null)
                    {
                        CvInvoke.PutText(image, "No PPE detected", new Point(10, 30),
                            Emgu.CV.CvEnum.FontFace.HersheySimplex, 1.0,
                            new Bgr(Color.Red).MCvScalar, 2);

                        pbVideoFeed.Image = image.ToBitmap();
                        return;
                    }
                    double scale = Math.Max(image.Width, image.Height) / 1000.0;  // 1000.0; 
                    scale = Math.Min(Math.Max(scale, 0.8 ), 3.0); // Clamp scale between 0.8 and 3.0
                    int thickness = (int)Math.Max(2, scale * 2);

                    // Draw PPE bounding boxes
                    foreach (var ppe in result.DetectedPpe)
                    {
                        CvInvoke.Rectangle(image, ppe.BoundingBox, new Bgr(Color.Blue).MCvScalar, 2);
                        CvInvoke.PutText(image, ppe.ClassLabel,
                            new Point(ppe.BoundingBox.X, ppe.BoundingBox.Y - 10),
                            Emgu.CV.CvEnum.FontFace.HersheySimplex, scale,
                            new Bgr(Color.Blue).MCvScalar, thickness);
                    }
                   
                    // Show missing PPE summary
                    var missingPpe = result.PpePresence
                        .Where(kv => !kv.Value)
                        .Select(kv => kv.Key.ToString())
                        .ToList();

                    string complianceText = result.IsCompliant
                        ? "Compliant"
                        : "Missing: " + string.Join(", ", missingPpe);

                    CvInvoke.PutText(image, complianceText, new Point(10, 30),
                        Emgu.CV.CvEnum.FontFace.HersheySimplex, 1.0,
                        result.IsCompliant ? new Bgr(Color.Green).MCvScalar : new Bgr(Color.Red).MCvScalar,
                        2);

                    // Snapshot for non-compliance
                    string snapshotPath = "N/A";
                    if (!result.IsCompliant)
                    {
                        string snapshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SiteGuardEdge_Snapshots");
                        if (!Directory.Exists(snapshotsDir))
                            Directory.CreateDirectory(snapshotsDir);

                        string fileName = $"SiteGuardEdge_{DateTime.Now:yyyyMMdd_HHmmss}_StillImage_NonCompliant_{Guid.NewGuid()}.png";
                        snapshotPath = Path.Combine(snapshotsDir, fileName);
                        image.Save(snapshotPath);
                    }

                    // Log detection event
                    await _detectionEventRepository.AddDetectionEventAsync(new DetectionEvent
                    {
                        Timestamp = DateTime.Now,
                        VideoSource = "Still Image",
                        FrameTimestamp = null,
                        PPE_Detected = string.Join(",", result.DetectedPpe.Select(p => p.ClassLabel)),
                        PPE_Missing = string.Join(",", missingPpe),
                        ComplianceStatus = result.IsCompliant ? "Compliant" : "Non-Compliant",
                        ConfidenceScore = result.DetectedPpe.Any() ? result.DetectedPpe.Max(p => p.Confidence) : 0,
                        BoundingBoxCoordinates = "N/A", // No person box in PPE-only mode
                        SnapshotPath = snapshotPath
                    });

                    // Display the processed image
                    pbVideoFeed.Image = image.ToBitmap();
                    lblFrameTimestamp.Text = $"Still Image: {Path.GetFileName(openFileDialogStill.FileName)}";

                    ////>>>>>>>>>>>>>>>>>..
                    // Perform PPE detection on the still image
                    //var ppeResults = await _ppeDetector.DetectPpeAsync(image.Mat);
                    //// check if any PPE results were found
                    //if (!ppeResults.Any()) 
                    //{ 
                    //    CvInvoke.PutText(image, "No person detected", new Point(10, 30), 
                    //    Emgu.CV.CvEnum.FontFace.HersheySimplex, 1.0, 
                    //    new Bgr(Color.Red).MCvScalar, 2); 
                    //    pbVideoFeed.Image = image.ToBitmap();
                    //    return; 
                    //}
                    //// Draw bounding boxes and labels on the image (reusing existing logic from ProcessFrame)
                    //foreach (var result in ppeResults)
                    //{
                    //    // Draw person bounding box
                    //    Color personBoxColor = result.IsCompliant ? Color.Green : Color.Red;
                    //    CvInvoke.Rectangle(image, result.Person.BoundingBox, new Bgr(personBoxColor).MCvScalar, 2);

                    //    // Draw PPE items and labels
                    //    foreach (var ppe in result.DetectedPpe)
                    //    {
                    //        CvInvoke.Rectangle(image, ppe.BoundingBox, new Bgr(Color.Blue).MCvScalar, 1);
                    //        CvInvoke.PutText(image, ppe.ClassLabel, new Point(ppe.BoundingBox.X, ppe.BoundingBox.Y - 5),
                    //                         Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new Bgr(Color.Blue).MCvScalar);
                    //    }

                    //    // Indicate missing PPE
                    //    var missingPpe = result.PpePresence.Where(kv => !kv.Value).Select(kv => kv.Key.ToString()).ToList();
                    //    if (missingPpe.Any())
                    //    {
                    //        string missingText = "Missing: " + string.Join(", ", missingPpe);
                    //        CvInvoke.PutText(image, missingText, new Point(result.Person.BoundingBox.X, result.Person.BoundingBox.Y + result.Person.BoundingBox.Height + 15),
                    //                         Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.6, new Bgr(Color.Red).MCvScalar, 2);
                    //    }

                    //    // Capture and store snapshot for non-compliant events
                    //    string snapshotPath = "N/A";
                    //    if (!result.IsCompliant)
                    //    {
                    //        string snapshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SiteGuardEdge_Snapshots");
                    //        if (!Directory.Exists(snapshotsDir))
                    //        {
                    //            Directory.CreateDirectory(snapshotsDir);
                    //        }
                    //        string fileName = $"SiteGuardEdge_{DateTime.Now:yyyyMMdd_HHmmss}_StillImage_NonCompliant_{Guid.NewGuid()}.png";
                    //        snapshotPath = Path.Combine(snapshotsDir, fileName);
                    //        image.Save(snapshotPath);
                    //    }

                //        // Log detection event
                //        await _detectionEventRepository.AddDetectionEventAsync(new DetectionEvent
                //        {
                //            Timestamp = DateTime.Now,
                //            VideoSource = "Still Image",
                //            FrameTimestamp = null, // No frame timestamp for still images
                //            PPE_Detected = string.Join(",", result.DetectedPpe.Select(p => p.ClassLabel)),
                //            PPE_Missing = string.Join(",", missingPpe),
                //            ComplianceStatus = result.IsCompliant ? "Compliant" : "Non-Compliant",
                //            ConfidenceScore = result.Person.Confidence,
                //            BoundingBoxCoordinates = $"{result.Person.BoundingBox.X},{result.Person.BoundingBox.Y},{result.Person.BoundingBox.Width},{result.Person.BoundingBox.Height}",
                //            SnapshotPath = snapshotPath
                //        });
                //    }

                //    // Display the processed image in pbVideoFeed
                //    pbVideoFeed.Image = image.ToBitmap();
                //    lblFrameTimestamp.Text = $"Still Image: {Path.GetFileName(openFileDialogStill.FileName)}";
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

    private async void btnExtractFrames_Click(object sender, EventArgs e)
    {
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.InitialDirectory = _recordingFolder; // Use our config variable
            openFileDialog.Filter = "Video Files (*.avi;*.mp4)|*.avi;*.mp4|All files (*.*)|*.*";
            openFileDialog.Title = "Select Video for Extraction";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = openFileDialog.FileName;

                // Architect's Choice: Ask user for mode (or use a RadioButton on your form)
                DialogResult mode = MessageBox.Show("Extract ALL frames? (No = Extract one specific frame)",
                                                  "Extraction Mode", MessageBoxButtons.YesNoCancel);

                if (mode == DialogResult.Yes)
                {
                    await Task.Run(() => RunExtractAll(selectedPath));
                }
                else if (mode == DialogResult.No)
                {
                    // For a specific frame, we'll just grab the middle frame as an example
                    // In a real app, you'd show a NumericUpDown for the frame index
                    await Task.Run(() => RunExtractSpecific(selectedPath, 100));
                }
            }
        }
    }
    private void RunExtractAll(string videoPath)
    {
        try
        {
            string subFolder = Path.Combine(_recordingFolder, $"Extracted_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(subFolder);

            using (VideoCapture capture = new VideoCapture(videoPath))
            using (Mat frame = new Mat())
            {
                int count = 0;
                double total = capture.Get(CapProp.FrameCount);

                LogAndStatus($"Starting extraction of {total} frames...");

                while (capture.Read(frame))
                {
                    if (frame.IsEmpty) break;

                    string framePath = Path.Combine(subFolder, $"frame_{count:D6}.jpg");
                    frame.Save(framePath);

                    count++;

                    // Only update status every 50 frames to prevent UI lag
                    if (count % 50 == 0)
                        LogAndStatus($"Extracted {count}/{total} frames...");
                }
            }
            LogAndStatus($"Success: All frames saved to {subFolder}");
        }
        catch (Exception ex)
        {
            LogAndStatus($"Extraction Error: {ex.Message}", true);
        }
    }
   
    private void RunExtractSpecific(string videoPath, int frameIndex)
    {
        try
        {
            using (VideoCapture capture = new VideoCapture(videoPath))
            using (Mat frame = new Mat())
            {
                capture.Set(CapProp.PosFrames, frameIndex);

                if (capture.Read(frame) && !frame.IsEmpty)
                {
                    string path = Path.Combine(_recordingFolder, $"SingleFrame_{frameIndex}.jpg");
                    frame.Save(path);
                    LogAndStatus($"Saved specific frame to: {path}");
                }
                else
                {
                    LogAndStatus("Could not find the requested frame index.", true);
                }
            }
        }
        catch (Exception ex)
        {
            LogAndStatus($"Extraction Error: {ex.Message}", true);
        }
    }
   

    public void ExtractAllFrames(string videoPath, string outputFolder)
    {
        using (VideoCapture capture = new VideoCapture(videoPath))
        using (Mat frame = new Mat())
        {
            int frameCount = 0;
            // Total frames in the file (useful for progress bars)
            double totalFrames = capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);

            while (capture.Read(frame)) // Read returns false when file ends
            {
                if (frame.IsEmpty) break;

                string fileName = Path.Combine(outputFolder, $"frame_{frameCount:D6}.jpg");
                frame.Save(fileName); // Saves the individual frame as a high-quality JPG

                frameCount++;
            }
        }
    }

    public void ExtractSpecificFrame(string videoPath, int frameIndex, string outputPath)
    {
        using (VideoCapture capture = new VideoCapture(videoPath))
        using (Mat frame = new Mat())
        {
            // Seek to the specific frame index
            capture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, frameIndex);

            if (capture.Read(frame))
            {
                frame.Save(outputPath);
            }
        }
    }
 

    private void LogAndStatus(string message, bool isError = false)
    {
        // 1. Update Status Bar (UI Thread)
        if (statusStrip1.InvokeRequired)
        {
           // lblStatus.GetCurrentParent().BeginInvoke(new Action(() => lblStatus.Text = message));
            statusStrip1.BeginInvoke(new Action(() => LogAndStatus(message, isError)));
        }
        else
        {
            lblStatus.Text = message;
        }

        // 2. Pipe to Log File
        string logPath = Path.Combine(System.Windows.Forms.Application.StartupPath, "app_log.txt");
        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(isError ? "ERROR" : "INFO")}: {message}{Environment.NewLine}";

        // Architect's Note: Simple append for now; for production, consider a background queue
        File.AppendAllText(logPath, logEntry);
    }

    private void LoadApplicationSettings()
    {
        try
        {
            // In a real app, you'd pull this from a JSON/XML config file or Database
            // For now, we'll use a configurable variable
            _recordingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "SiteGuardCaptures");

            if (!Directory.Exists(_recordingFolder))
            {
                Directory.CreateDirectory(_recordingFolder);
            }

            LogAndStatus($"Ready. Saving to: {_recordingFolder}");
        }
        catch (Exception ex)
        {
            LogAndStatus($"Configuration Error: {ex.Message}", true);
        }
    }

}


public class OnnxPersonDetector
{
    private readonly InferenceSession _session;
    private readonly int _inputWidth = 640;
    private readonly int _inputHeight = 640;

    public OnnxPersonDetector(string modelPath)
    {
        _session = new InferenceSession(modelPath);
    }

    public List<DetectedObject> DetectPersons(Mat image)
    {
        var tensor = Preprocess(image);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("images", tensor)
        };

        using var results = _session.Run(inputs);
        var output = results.First(x => x.Name == "output0").AsTensor<float>();

        return Parse(output, image.Width, image.Height);
    }


    // Remove 'unsafe' from the Preprocess method in OnnxPersonDetector
    private DenseTensor<float> Preprocess(Mat image)
    {
        using var resized = new Mat();
        CvInvoke.Resize(image, resized, new Size(_inputWidth, _inputHeight));

        var tensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

        byte[] data = resized.GetRawData();

        for (int y = 0; y < _inputHeight; y++)
        {
            for (int x = 0; x < _inputWidth; x++)
            {
                int idx = (y * resized.Cols + x) * resized.NumberOfChannels;

                tensor[0, 0, y, x] = data[idx + 2] / 255.0f; // R
                tensor[0, 1, y, x] = data[idx + 1] / 255.0f; // G
                tensor[0, 2, y, x] = data[idx + 0] / 255.0f; // B
            }
        }

        return tensor;
    }
    private List<DetectedObject> Parse(Tensor<float> output, int originalWidth, int originalHeight)
    {
        var detections = new List<DetectedObject>();
        int predictions = output.Dimensions[2];

        for (int i = 0; i < predictions; i++)
        {
            float x = output[0, 0, i];
            float y = output[0, 1, i];
            float w = output[0, 2, i];
            float h = output[0, 3, i];

            float objectness = output[0, 4, i];
            float personScore = output[0, 5 + 0, i]; // class 0 = person

            float confidence = objectness * personScore;

            if (confidence < 0.5f)
                continue;

            float x1 = (x - w / 2) / _inputWidth * originalWidth;
            float y1 = (y - h / 2) / _inputHeight * originalHeight;
            float bw = w / _inputWidth * originalWidth;
            float bh = h / _inputHeight * originalHeight;

            detections.Add(new DetectedObject
            {
                BoundingBox = new Rectangle((int)x1, (int)y1, (int)bw, (int)bh),
                ClassLabel = "person",
                Confidence = confidence,
                ClassId = 0
            });
        }

        return detections;
    }
}

