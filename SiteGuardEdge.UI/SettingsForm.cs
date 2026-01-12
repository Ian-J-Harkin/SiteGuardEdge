using SiteGuardEdge.Domain.Interfaces;

namespace SiteGuardEdge.UI;

public partial class SettingsForm : Form
{
    private readonly IConfigurationService _configurationService;

    public SettingsForm(IConfigurationService configurationService)
    {
        InitializeComponent();
        _configurationService = configurationService;
        this.Load += SettingsForm_Load;
    }

    private void SettingsForm_Load(object sender, EventArgs e)
    {
        PopulateRetentionPeriodDropdown();
        LoadCurrentSettings();
    }

    private void PopulateRetentionPeriodDropdown()
    {
        cbLogRetention.Items.Add("7 Days");
        cbLogRetention.Items.Add("30 Days");
        cbLogRetention.Items.Add("90 Days");
        cbLogRetention.Items.Add("Forever"); // Represented as a very large number of days or special value
    }

    private void LoadCurrentSettings()
    {
        TimeSpan currentRetention = _configurationService.GetLogRetentionPeriod();
        if (currentRetention == TimeSpan.FromDays(7))
        {
            cbLogRetention.SelectedItem = "7 Days";
        }
        else if (currentRetention == TimeSpan.FromDays(30))
        {
            cbLogRetention.SelectedItem = "30 Days";
        }
        else if (currentRetention == TimeSpan.FromDays(90))
        {
            cbLogRetention.SelectedItem = "90 Days";
        }
        else
        {
            cbLogRetention.SelectedItem = "Forever"; // Default or unrecognized
        }
    }

    private void btnSaveSettings_Click(object sender, EventArgs e)
    {
        TimeSpan selectedPeriod;
        switch (cbLogRetention.SelectedItem?.ToString())
        {
            case "7 Days":
                selectedPeriod = TimeSpan.FromDays(7);
                break;
            case "30 Days":
                selectedPeriod = TimeSpan.FromDays(30);
                break;
            case "90 Days":
                selectedPeriod = TimeSpan.FromDays(90);
                break;
            case "Forever":
                selectedPeriod = TimeSpan.FromDays(365 * 100); // A very long time
                break;
            default:
                selectedPeriod = TimeSpan.FromDays(30); // Default
                break;
        }

        _configurationService.SetLogRetentionPeriod(selectedPeriod);
        MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}