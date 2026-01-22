using Emgu.CV;
using Emgu.CV.Structure;
using SiteGuardEdge.Infrastructure.Video;

public class EmguCvVideoSource : IVideoSource, IDisposable
{
    public event EventHandler<(Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp)>? OnFrameReady;

    private VideoCapture _capture;
    private CancellationTokenSource _cts;
    private Task _captureTask;
    private readonly int? _deviceIndex;
    private readonly string? _filePath;

    public bool IsRunning => _captureTask != null && !_captureTask.IsCompleted && _cts != null && !_cts.IsCancellationRequested;
    public string? FilePath => _filePath;
    public bool IsFile => !string.IsNullOrEmpty(_filePath);

    public EmguCvVideoSource(int deviceIndex)
    { 
        _deviceIndex = deviceIndex;
        _filePath = null;
    }

    public EmguCvVideoSource(string filePath)
    {
        _deviceIndex = null;
        _filePath = filePath;    
    }

    public async Task StartAsync()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();

        // Create capture here so StartAsync can fail early if needed
        _capture = _deviceIndex.HasValue
            ? new VideoCapture(_deviceIndex.Value)
            : new VideoCapture(_filePath);

        if (!_capture.IsOpened)
        {
            _capture.Dispose();
            _capture = null;
            throw new InvalidOperationException("Failed to open video source.");
        }

        _captureTask = Task.Run(() => CaptureLoopAsync(_cts.Token), _cts.Token);

        await Task.Yield(); // allow task to start
    }


    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts.Cancel();

        try
        {
            await _captureTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        _capture?.Dispose();
        _capture = null;

        _cts.Dispose();
        _cts = null;
        _captureTask = null;
    }

    private async Task CaptureLoopAsync(CancellationToken token)
    {
        try
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested)
            {
                _capture.Read(frame);

                if (!frame.IsEmpty)
                {
                    int frameNumber = (int)_capture.Get(Emgu.CV.CvEnum.CapProp.PosFrames);
                    int totalFrames = (int)_capture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    double fps = _capture.Get(Emgu.CV.CvEnum.CapProp.Fps);

                    TimeSpan? timestamp = IsFile && fps > 0
                        ? TimeSpan.FromSeconds(frameNumber / fps)
                        : null;

                    OnFrameReady?.Invoke(this, (frame.ToImage<Bgr, byte>(), frameNumber, totalFrames, timestamp));
                }
                else if (IsFile)
                {
                    break; // end of file
                }

                await Task.Delay(30, token).ConfigureAwait(false);
            }
        }
        finally
        {
            _capture?.Dispose();
            _capture = null;
        }
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}