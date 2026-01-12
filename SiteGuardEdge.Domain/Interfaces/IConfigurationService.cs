namespace SiteGuardEdge.Domain.Interfaces;

public interface IConfigurationService
{
    TimeSpan GetLogRetentionPeriod();
    void SetLogRetentionPeriod(TimeSpan period);
}