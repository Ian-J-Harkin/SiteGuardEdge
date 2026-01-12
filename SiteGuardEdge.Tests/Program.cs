using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.IO;

namespace SiteGuardEdge.Tests;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Running Emgu.CV video frame reading test...");

        // Create a dummy video file for testing
        string dummyVideoPath = "dummy_video.avi";
        CreateDummyVideoFile(dummyVideoPath);

        try
        {
            using (VideoCapture capture = new VideoCapture(dummyVideoPath))
            {
                if (!capture.IsOpened)
                {
                    Console.WriteLine("Error: Could not open video file.");
                    return;
                }

                Mat frame = new Mat();
                if (capture.Read(frame))
                {
                    Console.WriteLine($"Successfully read a frame of size: {frame.Size.Width}x{frame.Size.Height}");
                }
                else
                {
                    Console.WriteLine("Could not read frame from video file.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            // Clean up the dummy video file
            if (File.Exists(dummyVideoPath))
            {
                File.Delete(dummyVideoPath);
                Console.WriteLine($"Cleaned up dummy video file: {dummyVideoPath}");
            }
        }

        Console.WriteLine("Emgu.CV video frame reading test completed.");
    }

    static void CreateDummyVideoFile(string path)
    {
        // This is a very basic dummy file creation.
        // For a real test, you might need a proper video file.
        // This just creates an empty file, which VideoCapture will likely fail on,
        // but it serves to test if the Emgu.CV library can be initialized and
        // attempts to open a file without crashing due to missing native libs.
        // A more robust test would involve a small, actual video file.
        try
        {
            File.WriteAllBytes(path, new byte[100]); // Create a small, empty file
            Console.WriteLine($"Created dummy video file: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating dummy video file: {ex.Message}");
        }
    }
}
