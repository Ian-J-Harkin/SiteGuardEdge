using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;

namespace SiteGuardEdge.Application;

public class GPU_CPU_Detector
{
    public static bool IsCudaGpuAvailable()
    {
        try
        {
            // Attempt to create a session with CUDA execution provider
            // This will throw an exception if CUDA is not available or configured incorrectly
            using (var sessionOptions = new SessionOptions())
            {
                sessionOptions.AppendExecutionProvider_CUDA();
                // No need to actually create a session, just checking if the provider can be appended
                return true;
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            Console.WriteLine($"CUDA GPU detection failed: {ex.Message}");
            return false;
        }
    }

    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}