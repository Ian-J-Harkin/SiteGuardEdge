using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SiteGuardEdge.Infrastructure.Video;

public interface IVideoSource : IDisposable
{
    event EventHandler<(Image<Bgr, byte> Image, int FrameNumber, int TotalFrames, TimeSpan? Timestamp)> OnFrameReady;
    Task StartAsync();
    Task StopAsync();
    bool IsRunning { get; }
    string FilePath { get; }
    bool IsFile { get; }
    static List<VideoDevice> GetAvailableDevices() => throw new NotImplementedException();
}

public class VideoDevice
{
    public string Name { get; set; }
    public int Index { get; set; }

    public override string ToString()
    {
        return Name;
    }
}