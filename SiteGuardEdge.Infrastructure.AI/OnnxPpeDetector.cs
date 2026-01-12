using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using SiteGuardEdge.Application; // For GPU_CPU_Detector
using System.Threading.Tasks;
using Emgu.CV.Util;

namespace SiteGuardEdge.Infrastructure.AI;

public enum PpeItem
{
    Helmet,
    Vest,
    Gloves,
    SafetyGlasses
}

public class DetectedObject
{
    public Rectangle BoundingBox { get; set; }
    public string ClassLabel { get; set; }
    public float Confidence { get; set; }
    public int ClassId { get; set; }
}

public class PpeComplianceResult
{
    public DetectedObject Person { get; set; }
    public bool IsCompliant { get; set; }
    public Dictionary<PpeItem, bool> PpePresence { get; set; } = new Dictionary<PpeItem, bool>();
    public List<DetectedObject> DetectedPpe { get; set; } = new List<DetectedObject>();
}

public class OnnxPpeDetector
{
    private readonly InferenceSession _session;
    private readonly string _modelPath;
    private readonly bool _useGpu;
    private readonly string _logMessage;
    private readonly float _confidenceThreshold;
    private readonly string[] _classNames = { "person", "helmet", "vest", "gloves", "safety glasses" }; // Example class names, adjust as per your model
    private readonly int _inputWidth = 640;
    private readonly int _inputHeight = 640;

    public OnnxPpeDetector(string modelPath, float confidenceThreshold = 0.5f, bool forceCpu = false, bool forceGpu = false)
    {
        _modelPath = modelPath;
        _confidenceThreshold = confidenceThreshold;
        _useGpu = false; // Default to CPU

        SessionOptions options = new SessionOptions();

        if (forceGpu)
        {
            if (GPU_CPU_Detector.IsCudaGpuAvailable())
            {
                options.AppendExecutionProvider_CUDA();
                _useGpu = true;
                _logMessage = "ONNX Runtime initialized with GPU (CUDA) mode.";
            }
            else
            {
                _logMessage = "ONNX Runtime initialized with CPU mode (GPU forced but not available).";
            }
        }
        else if (forceCpu)
        {
            _logMessage = "ONNX Runtime initialized with CPU mode (CPU forced).";
        }
        else // Auto-detection
        {
            if (GPU_CPU_Detector.IsCudaGpuAvailable())
            {
                options.AppendExecutionProvider_CUDA();
                _useGpu = true;
                _logMessage = "ONNX Runtime initialized with GPU (CUDA) mode (auto-detected).";
            }
            else
            {
                _logMessage = "ONNX Runtime initialized with CPU mode (auto-detected).";
            }
        }

        Console.WriteLine(_logMessage); // Log the active mode

        _session = new InferenceSession(_modelPath, options);
    }

    public async Task<List<PpeComplianceResult>> DetectPpeAsync(Mat image)
    {
        // Pre-processing
        using var resizedImage = new Mat();
        CvInvoke.Resize(image, resizedImage, new Size(_inputWidth, _inputHeight));

        var inputTensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });

        // Convert BGR to RGB and normalize to [0, 1]
        // Emgu.CV.Mat stores data in BGR format by default
        unsafe
        {
            byte* dataPtr = (byte*)resizedImage.DataPointer;
            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    int baseIndex = (y * resizedImage.Cols + x) * resizedImage.NumberOfChannels;
                    inputTensor[0, 0, y, x] = dataPtr[baseIndex + 2] / 255.0f; // R
                    inputTensor[0, 1, y, x] = dataPtr[baseIndex + 1] / 255.0f; // G
                    inputTensor[0, 2, y, x] = dataPtr[baseIndex + 0] / 255.0f; // B
                }
            }
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("images", inputTensor)
        };

        using (var results = await Task.Run(() => _session.Run(inputs)))
        {
            var output = results.FirstOrDefault(x => x.Name == "output0")?.AsTensor<float>(); // YOLOv8 output name is often "output0"

            if (output == null)
            {
                return new List<PpeComplianceResult>();
            }

            // Post-processing: Parse model output, apply NMS, and determine compliance
            var detectedObjects = ParseYoloOutput(output, image.Width, image.Height);
            var ppeComplianceResults = AnalyzePpeCompliance(detectedObjects);

            return ppeComplianceResults;
        }
    }

    private List<DetectedObject> ParseYoloOutput(Tensor<float> output, int originalWidth, int originalHeight)
    {
        var detections = new List<DetectedObject>();
        // YOLOv8 output format: [1, 84, 8400] for 80 classes (x, y, w, h, conf, 80 class_scores)
        // Or [1, num_boxes, 84] if transposed
        // Assuming output is [1, 84, N] where N is number of predictions.

        int numClasses = _classNames.Length; // 5 classes: person, helmet, vest, gloves, safety glasses
        int predictionsCount = output.Dimensions[2]; // N

        for (int i = 0; i < predictionsCount; i++)
        {
            float x = output[0, 0, i];
            float y = output[0, 1, i];
            float w = output[0, 2, i];
            float h = output[0, 3, i];
            float objectConfidence = output[0, 4, i];

            // Find the class with the highest score
            float maxClassScore = 0;
            int classId = -1;
            for (int j = 0; j < numClasses; j++)
            {
                float classScore = output[0, 5 + j, i];
                if (classScore > maxClassScore)
                {
                    maxClassScore = classScore;
                    classId = j;
                }
            }

            float confidence = objectConfidence * maxClassScore;

            if (confidence >= _confidenceThreshold && classId != -1)
            {
                // Convert YOLO format (center_x, center_y, width, height) to (x, y, width, height)
                float x1 = (x - w / 2) / _inputWidth * originalWidth;
                float y1 = (y - h / 2) / _inputHeight * originalHeight;
                float boxWidth = w / _inputWidth * originalWidth;
                float boxHeight = h / _inputHeight * originalHeight;

                detections.Add(new DetectedObject
                {
                    BoundingBox = new Rectangle((int)x1, (int)y1, (int)boxWidth, (int)boxHeight),
                    ClassLabel = _classNames[classId],
                    Confidence = confidence,
                    ClassId = classId
                });
            }
        }

        return ApplyNms(detections);
    }

    private List<DetectedObject> ApplyNms(List<DetectedObject> detections, float iouThreshold = 0.45f)
    {
        var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();
        var suppressed = new bool[sortedDetections.Count];
        var nmsDetections = new List<DetectedObject>();

        for (int i = 0; i < sortedDetections.Count; i++)
        {
            if (suppressed[i]) continue;

            var current = sortedDetections[i];
            nmsDetections.Add(current);

            for (int j = i + 1; j < sortedDetections.Count; j++)
            {
                if (suppressed[j]) continue;

                var other = sortedDetections[j];
                if (current.ClassId == other.ClassId && CalculateIoU(current.BoundingBox, other.BoundingBox) > iouThreshold)
                {
                    suppressed[j] = true;
                }
            }
        }
        return nmsDetections;
    }

    private float CalculateIoU(Rectangle box1, Rectangle box2)
    {
        int xA = Math.Max(box1.Left, box2.Left);
        int yA = Math.Max(box1.Top, box2.Top);
        int xB = Math.Min(box1.Right, box2.Right);
        int yB = Math.Min(box1.Bottom, box2.Bottom);

        int intersectionArea = Math.Max(0, xB - xA) * Math.Max(0, yB - yA);

        int box1Area = box1.Width * box1.Height;
        int box2Area = box2.Width * box2.Height;

        float iou = (float)intersectionArea / (box1Area + box2Area - intersectionArea);
        return iou;
    }

    private List<PpeComplianceResult> AnalyzePpeCompliance(List<DetectedObject> detections, float iouThreshold = 0.2f)
    {
        var complianceResults = new List<PpeComplianceResult>();
        var persons = detections.Where(d => d.ClassLabel == "person").ToList();
        var ppeItems = detections.Where(d => d.ClassLabel != "person").ToList();

        foreach (var person in persons)
        {
            var ppePresence = new Dictionary<PpeItem, bool>
            {
                { PpeItem.Helmet, false },
                { PpeItem.Vest, false },
                { PpeItem.Gloves, false },
                { PpeItem.SafetyGlasses, false }
            };
            var detectedPpeForPerson = new List<DetectedObject>();

            foreach (var ppe in ppeItems)
            {
                if (CalculateIoU(person.BoundingBox, ppe.BoundingBox) > iouThreshold)
                {
                    detectedPpeForPerson.Add(ppe);
                    switch (ppe.ClassLabel)
                    {
                        case "helmet":
                            ppePresence[PpeItem.Helmet] = true;
                            break;
                        case "vest":
                            ppePresence[PpeItem.Vest] = true;
                            break;
                        case "gloves":
                            ppePresence[PpeItem.Gloves] = true;
                            break;
                        case "safety glasses":
                            ppePresence[PpeItem.SafetyGlasses] = true;
                            break;
                    }
                }
            }

            bool isCompliant = ppePresence.All(kv => kv.Value); // All required PPE items must be present

            complianceResults.Add(new PpeComplianceResult
            {
                Person = person,
                IsCompliant = isCompliant,
                PpePresence = ppePresence,
                DetectedPpe = detectedPpeForPerson
            });
        }

        return complianceResults;
    }
}