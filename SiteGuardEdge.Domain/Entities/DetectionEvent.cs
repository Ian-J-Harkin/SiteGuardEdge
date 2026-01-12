namespace SiteGuardEdge.Domain.Entities;

public class DetectionEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string VideoSource { get; set; }
    public TimeSpan? FrameTimestamp { get; set; }
    public string PPE_Detected { get; set; } // JSON string or comma-separated
    public string PPE_Missing { get; set; } // JSON string or comma-separated
    public string ComplianceStatus { get; set; } // "Compliant" or "Non-Compliant"
    public float ConfidenceScore { get; set; }
    public string BoundingBoxCoordinates { get; set; } // JSON string of bounding boxes
    public string SnapshotPath { get; set; }
}