using Microsoft.EntityFrameworkCore;
using SiteGuardEdge.Domain.Entities;
using SiteGuardEdge.Domain.Interfaces;

namespace SiteGuardEdge.Infrastructure.Data.Repositories;

public class DetectionEventRepository : IDetectionEventRepository
{
    private readonly SiteGuardEdgeDbContext _context;

    public DetectionEventRepository(SiteGuardEdgeDbContext context)
    {
        _context = context;
    }

    public async Task AddDetectionEventAsync(DetectionEvent detectionEvent)
    {
        await _context.DetectionEvents.AddAsync(detectionEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DetectionEvent>> GetAllDetectionEventsAsync()
    {
        return await _context.DetectionEvents.ToListAsync();
    }

    public async Task<IEnumerable<DetectionEvent>> GetFilteredDetectionEventsAsync(string? complianceStatus = null, DateTime? startDate = null, DateTime? endDate = null, string? videoSource = null)
    {
        IQueryable<DetectionEvent> query = _context.DetectionEvents;

        if (!string.IsNullOrEmpty(complianceStatus))
        {
            query = query.Where(e => e.ComplianceStatus == complianceStatus);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(videoSource))
        {
            query = query.Where(e => e.VideoSource.Contains(videoSource));
        }

        return await query.ToListAsync();
    }

    public async Task PurgeOldDetectionEventsAsync(TimeSpan retentionPeriod)
    {
        var cutoffDate = DateTime.Now.Subtract(retentionPeriod);
        var oldEvents = await _context.DetectionEvents.Where(e => e.Timestamp < cutoffDate).ToListAsync();
        _context.DetectionEvents.RemoveRange(oldEvents);
        await _context.SaveChangesAsync();
    }
}