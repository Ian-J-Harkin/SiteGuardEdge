using SiteGuardEdge.Domain.Entities;

namespace SiteGuardEdge.Domain.Interfaces;

public interface IDetectionEventRepository
{
    Task AddDetectionEventAsync(DetectionEvent detectionEvent);
    Task<IEnumerable<DetectionEvent>> GetAllDetectionEventsAsync();
    Task<IEnumerable<DetectionEvent>> GetFilteredDetectionEventsAsync(string? complianceStatus = null, DateTime? startDate = null, DateTime? endDate = null, string? videoSource = null);
    Task PurgeOldDetectionEventsAsync(TimeSpan retentionPeriod);
}