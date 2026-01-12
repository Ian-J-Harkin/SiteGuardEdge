namespace SiteGuardEdge.UI;

partial class LogViewerForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.dgvLogs = new System.Windows.Forms.DataGridView();
        this.lblComplianceStatus = new System.Windows.Forms.Label();
        this.cbComplianceStatus = new System.Windows.Forms.ComboBox();
        this.lblStartDate = new System.Windows.Forms.Label();
        this.dtpStartDate = new System.Windows.Forms.DateTimePicker();
        this.lblEndDate = new System.Windows.Forms.Label();
        this.dtpEndDate = new System.Windows.Forms.DateTimePicker();
        this.lblVideoSourceFilter = new System.Windows.Forms.Label();
        this.txtVideoSourceFilter = new System.Windows.Forms.TextBox();
        this.btnApplyFilters = new System.Windows.Forms.Button();
        this.btnClearFilters = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this.dgvLogs)).BeginInit();
        this.SuspendLayout();
        // 
        // dgvLogs
        // 
        this.dgvLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
        this.dgvLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvLogs.Location = new System.Drawing.Point(12, 90);
        this.dgvLogs.Name = "dgvLogs";
        this.dgvLogs.RowTemplate.Height = 25;
        this.dgvLogs.Size = new System.Drawing.Size(776, 348);
        this.dgvLogs.TabIndex = 0;
        // 
        // lblComplianceStatus
        // 
        this.lblComplianceStatus.AutoSize = true;
        this.lblComplianceStatus.Location = new System.Drawing.Point(12, 15);
        this.lblComplianceStatus.Name = "lblComplianceStatus";
        this.lblComplianceStatus.Size = new System.Drawing.Size(107, 15);
        this.lblComplianceStatus.TabIndex = 1;
        this.lblComplianceStatus.Text = "Compliance Status:";
        // 
        // cbComplianceStatus
        // 
        this.cbComplianceStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cbComplianceStatus.FormattingEnabled = true;
        this.cbComplianceStatus.Location = new System.Drawing.Point(125, 12);
        this.cbComplianceStatus.Name = "cbComplianceStatus";
        this.cbComplianceStatus.Size = new System.Drawing.Size(121, 23);
        this.cbComplianceStatus.TabIndex = 2;
        // 
        // lblStartDate
        // 
        this.lblStartDate.AutoSize = true;
        this.lblStartDate.Location = new System.Drawing.Point(260, 15);
        this.lblStartDate.Name = "lblStartDate";
        this.lblStartDate.Size = new System.Drawing.Size(61, 15);
        this.lblStartDate.TabIndex = 3;
        this.lblStartDate.Text = "Start Date:";
        // 
        // dtpStartDate
        // 
        this.dtpStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpStartDate.Location = new System.Drawing.Point(327, 12);
        this.dtpStartDate.Name = "dtpStartDate";
        this.dtpStartDate.Size = new System.Drawing.Size(100, 23);
        this.dtpStartDate.TabIndex = 4;
        // 
        // lblEndDate
        // 
        this.lblEndDate.AutoSize = true;
        this.lblEndDate.Location = new System.Drawing.Point(440, 15);
        this.lblEndDate.Name = "lblEndDate";
        this.lblEndDate.Size = new System.Drawing.Size(57, 15);
        this.lblEndDate.TabIndex = 5;
        this.lblEndDate.Text = "End Date:";
        // 
        // dtpEndDate
        // 
        this.dtpEndDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.dtpEndDate.Location = new System.Drawing.Point(503, 12);
        this.dtpEndDate.Name = "dtpEndDate";
        this.dtpEndDate.Size = new System.Drawing.Size(100, 23);
        this.dtpEndDate.TabIndex = 6;
        // 
        // lblVideoSourceFilter
        // 
        this.lblVideoSourceFilter.AutoSize = true;
        this.lblVideoSourceFilter.Location = new System.Drawing.Point(12, 50);
        this.lblVideoSourceFilter.Name = "lblVideoSourceFilter";
        this.lblVideoSourceFilter.Size = new System.Drawing.Size(77, 15);
        this.lblVideoSourceFilter.TabIndex = 7;
        this.lblVideoSourceFilter.Text = "Video Source:";
        // 
        // txtVideoSourceFilter
        // 
        this.txtVideoSourceFilter.Location = new System.Drawing.Point(95, 47);
        this.txtVideoSourceFilter.Name = "txtVideoSourceFilter";
        this.txtVideoSourceFilter.Size = new System.Drawing.Size(151, 23);
        this.txtVideoSourceFilter.TabIndex = 8;
        // 
        // btnApplyFilters
        // 
        this.btnApplyFilters.Location = new System.Drawing.Point(610, 12);
        this.btnApplyFilters.Name = "btnApplyFilters";
        this.btnApplyFilters.Size = new System.Drawing.Size(85, 23);
        this.btnApplyFilters.TabIndex = 9;
        this.btnApplyFilters.Text = "Apply Filters";
        this.btnApplyFilters.UseVisualStyleBackColor = true;
        this.btnApplyFilters.Click += new System.EventHandler(this.btnApplyFilters_Click);
        // 
        // btnClearFilters
        // 
        this.btnClearFilters.Location = new System.Drawing.Point(703, 12);
        this.btnClearFilters.Name = "btnClearFilters";
        this.btnClearFilters.Size = new System.Drawing.Size(85, 23);
        this.btnClearFilters.TabIndex = 10;
        this.btnClearFilters.Text = "Clear Filters";
        this.btnClearFilters.UseVisualStyleBackColor = true;
        this.btnClearFilters.Click += new System.EventHandler(this.btnClearFilters_Click);
        // 
        // LogViewerForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.btnClearFilters);
        this.Controls.Add(this.btnApplyFilters);
        this.Controls.Add(this.txtVideoSourceFilter);
        this.Controls.Add(this.lblVideoSourceFilter);
        this.Controls.Add(this.dtpEndDate);
        this.Controls.Add(this.lblEndDate);
        this.Controls.Add(this.dtpStartDate);
        this.Controls.Add(this.lblStartDate);
        this.Controls.Add(this.cbComplianceStatus);
        this.Controls.Add(this.lblComplianceStatus);
        this.Controls.Add(this.dgvLogs);
        this.Name = "LogViewerForm";
        this.Text = "Detection Logs";
        ((System.ComponentModel.ISupportInitialize)(this.dgvLogs)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    private System.Windows.Forms.DataGridView dgvLogs;
    private System.Windows.Forms.Label lblComplianceStatus;
    private System.Windows.Forms.ComboBox cbComplianceStatus;
    private System.Windows.Forms.Label lblStartDate;
    private System.Windows.Forms.DateTimePicker dtpStartDate;
    private System.Windows.Forms.Label lblEndDate;
    private System.Windows.Forms.DateTimePicker dtpEndDate;
    private System.Windows.Forms.Label lblVideoSourceFilter;
    private System.Windows.Forms.TextBox txtVideoSourceFilter;
    private System.Windows.Forms.Button btnApplyFilters;
    private System.Windows.Forms.Button btnClearFilters;

    #endregion
}