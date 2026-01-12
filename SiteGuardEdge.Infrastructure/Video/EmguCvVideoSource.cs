using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace SiteGuardEdge.Infrastructure.Video;

public class EmguCvVideoSource : IVideoSource
{
    public event EventHandler<(Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp)> OnFrameReady;

    private VideoCapture _capture;
    private CancellationTokenSource _cts;
    private Task _captureTask;
    private readonly int? _deviceIndex;
    private readonly string _filePath;

    public bool IsRunning => _captureTask != null && !_cts.IsCancellationRequested;
    public string FilePath => _filePath;
    public bool IsFile => !string.IsNullOrEmpty(_filePath);

    // Constructor for webcam
    public EmguCvVideoSource(int deviceIndex)
    {
        _deviceIndex = deviceIndex;
    }

    // Constructor for video file
    public EmguCvVideoSource(string filePath)
    {
        _filePath = filePath;
    }

    public static List<VideoDevice> GetAvailableDevices()
    {
        var devices = new List<VideoDevice>();
        // Emgu.CV does not provide a direct way to list device names.
        // We can try to open devices by index and if successful, assume it's available.
        // This is a heuristic and might not be perfect.
        for (int i = 0; i < 10; i++) // Try up to 10 devices
        {
            try
            {
                using (var tempCapture = new VideoCapture(i))
                {
                    if (tempCapture.IsOpened)
                    {
                        devices.Add(new VideoDevice { Name = $"Webcam {i + 1}", Index = i });
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors, device might not exist or be in use
            }
        }
        return devices;
    }

    public async Task StartAsync()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _captureTask = Task.Run(() => CaptureLoop(_cts.Token), _cts.Token);
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        if (_captureTask != null)
        {
            await _captureTask;
        }
        _capture?.Dispose();
        _capture = null;
        _cts?.Dispose();
        _cts = null;
    }

    private void CaptureLoop(CancellationToken token)
    {
        try
        {
            if (_deviceIndex.HasValue)
            {
                _capture = new VideoCapture(_deviceIndex.Value);
            }
            else if (!string.IsNullOrEmpty(_filePath))
            {
                _capture = new VideoCapture(_filePath);
            }
            else
            {
                throw new InvalidOperationException("No video source specified.");
            }

            if (!_capture.IsOpened)
            {
                throw new InvalidOperationException("Failed to open video source.");
            }

            Mat frame = new Mat();
            while (!token.IsCancellationRequested)
            {
                _capture.Read(frame);
                if (!frame.IsEmpty)
                {
                    int frameNumber = (int)_capture.Get(Emgu.CV.CvEnum.CapProp.PosFrames);
                    int totalFrames = (int)_capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    double fps = _capture.Get(Emgu.CV.CvEnum.CapProp.Fps);
                    TimeSpan? timestamp = null;

                    if (IsFile && fps > 0)
                    {
                        timestamp = TimeSpan.FromSeconds(frameNumber / fps);
                    }

                    OnFrameReady?.Invoke(this, (frame.ToImage<Bgr, byte>(), frameNumber, totalFrames, timestamp));
                }
                else if (IsFile)
                {
                    // If it's a file and we reached the end, loop or stop
                    // For now, just stop for simplicity
                    break;
                }
                Task.Delay(30).Wait(); // Simulate ~30 FPS for display, actual processing rate is different
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Video capture error: {ex.Message}");
            // Optionally re-throw or notify UI
        }
        finally
        {
            _capture?.Dispose();
            _capture = null;
        }
    }

    public void Dispose()
    {
        StopAsync().Wait(); // Ensure capture is stopped and resources are released
    }
}