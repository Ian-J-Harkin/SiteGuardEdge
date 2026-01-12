using SiteGuardEdge.Domain.Interfaces;
using System.Configuration;

namespace SiteGuardEdge.Application;

public class ConfigurationService : IConfigurationService
{
    private const string LogRetentionKey = "LogRetentionDays";

    public TimeSpan GetLogRetentionPeriod()
    {
        if (int.TryParse(ConfigurationManager.AppSettings[LogRetentionKey], out int days))
        {
            return TimeSpan.FromDays(days);
        }
        return TimeSpan.FromDays(30); // Default to 30 days
    }

    public void SetLogRetentionPeriod(TimeSpan period)
    {
        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        if (config.AppSettings.Settings[LogRetentionKey] == null)
        {
            config.AppSettings.Settings.Add(LogRetentionKey, period.TotalDays.ToString());
        }
        else
        {
            config.AppSettings.Settings[LogRetentionKey].Value = period.TotalDays.ToString();
        }
        config.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
    }
}