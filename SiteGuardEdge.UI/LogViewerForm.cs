using SiteGuardEdge.Domain.Interfaces;
using SiteGuardEdge.Domain.Entities;
using System.Data;

namespace SiteGuardEdge.UI;

public partial class LogViewerForm : Form
{
    private readonly IDetectionEventRepository _detectionEventRepository;

    public LogViewerForm(IDetectionEventRepository detectionEventRepository)
    {
        InitializeComponent();
        _detectionEventRepository = detectionEventRepository;
        this.Load += LogViewerForm_Load;
    }

    private async void LogViewerForm_Load(object sender, EventArgs e)
    {
        await LoadLogs();
        PopulateFilterOptions();
    }

    private async Task LoadLogs(string? complianceStatus = null, DateTime? startDate = null, DateTime? endDate = null, string? videoSource = null)
    {
        try
        {
            var logs = await _detectionEventRepository.GetFilteredDetectionEventsAsync(complianceStatus, startDate, endDate, videoSource);
            dgvLogs.DataSource = logs.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateFilterOptions()
    {
        // Populate Compliance Status ComboBox
        cbComplianceStatus.Items.Add("All");
        cbComplianceStatus.Items.Add("Compliant");
        cbComplianceStatus.Items.Add("Non-Compliant");
        cbComplianceStatus.SelectedIndex = 0;

        // Set DateTimePickers to a reasonable range or default
        dtpStartDate.Value = DateTime.Today.AddMonths(-1);
        dtpEndDate.Value = DateTime.Today;
    }

    private async void btnApplyFilters_Click(object sender, EventArgs e)
    {
        string? complianceStatus = cbComplianceStatus.SelectedItem?.ToString();
        if (complianceStatus == "All") complianceStatus = null;

        DateTime? startDate = dtpStartDate.Value.Date;
        DateTime? endDate = dtpEndDate.Value.Date.AddDays(1).AddTicks(-1); // End of day

        string? videoSource = txtVideoSourceFilter.Text.Trim();
        if (string.IsNullOrEmpty(videoSource)) videoSource = null;

        await LoadLogs(complianceStatus, startDate, endDate, videoSource);
    }

    private async void btnClearFilters_Click(object sender, EventArgs e)
    {
        cbComplianceStatus.SelectedIndex = 0;
        dtpStartDate.Value = DateTime.Today.AddMonths(-1);
        dtpEndDate.Value = DateTime.Today;
        txtVideoSourceFilter.Text = string.Empty;
        await LoadLogs();
    }
}