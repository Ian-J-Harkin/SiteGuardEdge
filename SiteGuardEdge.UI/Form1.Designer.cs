namespace SiteGuardEdge.UI;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        cbWebcamDevices = new ComboBox();
        btnStartStop = new Button();
        pbVideoFeed = new PictureBox();
        btnBrowseVideo = new Button();
        openFileDialogStill = new OpenFileDialog();
        btnViewLogs = new Button();
        btnSettings = new Button();
        lblFrameTimestamp = new Label();
        btnStill = new Button();
        btnExtractFrames = new Button();
        statusStrip1 = new StatusStrip();
        toolStrip1 = new ToolStrip();
        lblStatus = new ToolStripLabel();
        ((System.ComponentModel.ISupportInitialize)pbVideoFeed).BeginInit();
        toolStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // cbWebcamDevices
        // 
        cbWebcamDevices.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cbWebcamDevices.DropDownStyle = ComboBoxStyle.DropDownList;
        cbWebcamDevices.FormattingEnabled = true;
        cbWebcamDevices.Location = new Point(12, 12);
        cbWebcamDevices.Name = "cbWebcamDevices";
        cbWebcamDevices.Size = new Size(676, 23);
        cbWebcamDevices.TabIndex = 0;
        // 
        // btnStartStop
        // 
        btnStartStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnStartStop.Location = new Point(694, 12);
        btnStartStop.Name = "btnStartStop";
        btnStartStop.Size = new Size(100, 23);
        btnStartStop.TabIndex = 1;
        btnStartStop.Text = "Start Webcam";
        btnStartStop.UseVisualStyleBackColor = true;
        btnStartStop.Click += btnStartStop_Click;
        // 
        // pbVideoFeed
        // 
        pbVideoFeed.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pbVideoFeed.BorderStyle = BorderStyle.FixedSingle;
        pbVideoFeed.Location = new Point(12, 41);
        pbVideoFeed.Name = "pbVideoFeed";
        pbVideoFeed.Size = new Size(676, 446);
        pbVideoFeed.SizeMode = PictureBoxSizeMode.Zoom;
        pbVideoFeed.TabIndex = 2;
        pbVideoFeed.TabStop = false;
        // 
        // btnBrowseVideo
        // 
        btnBrowseVideo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseVideo.Location = new Point(482, 12);
        btnBrowseVideo.Name = "btnBrowseVideo";
        btnBrowseVideo.Size = new Size(100, 23);
        btnBrowseVideo.TabIndex = 2;
        btnBrowseVideo.Text = "Browse Video";
        btnBrowseVideo.UseVisualStyleBackColor = true;
        btnBrowseVideo.Click += btnBrowseVideo_Click;
        // 
        // openFileDialogStill
        // 
        openFileDialogStill.Filter = "Video Files|*.mp4;*.avi;*.mov;*.mkv|All Files|*.*";
        // 
        // btnViewLogs
        // 
        btnViewLogs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnViewLogs.Location = new Point(694, 99);
        btnViewLogs.Name = "btnViewLogs";
        btnViewLogs.Size = new Size(100, 23);
        btnViewLogs.TabIndex = 5;
        btnViewLogs.Text = "View Logs";
        btnViewLogs.UseVisualStyleBackColor = true;
        btnViewLogs.Click += btnViewLogs_Click;
        // 
        // btnSettings
        // 
        btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSettings.Location = new Point(694, 128);
        btnSettings.Name = "btnSettings";
        btnSettings.Size = new Size(100, 23);
        btnSettings.TabIndex = 6;
        btnSettings.Text = "Settings";
        btnSettings.UseVisualStyleBackColor = true;
        btnSettings.Click += btnSettings_Click;
        // 
        // lblFrameTimestamp
        // 
        lblFrameTimestamp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblFrameTimestamp.AutoSize = true;
        lblFrameTimestamp.Location = new Point(12, 490);
        lblFrameTimestamp.Name = "lblFrameTimestamp";
        lblFrameTimestamp.Size = new Size(0, 15);
        lblFrameTimestamp.TabIndex = 4;
        // 
        // btnStill
        // 
        btnStill.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnStill.Location = new Point(694, 70);
        btnStill.Name = "btnStill";
        btnStill.Size = new Size(100, 23);
        btnStill.TabIndex = 7;
        btnStill.Text = "Browse Stills";
        btnStill.UseVisualStyleBackColor = true;
        btnStill.Click += btnStill_Click;
        // 
        // btnExtractFrames
        // 
        btnExtractFrames.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExtractFrames.Location = new Point(694, 41);
        btnExtractFrames.Name = "btnExtractFrames";
        btnExtractFrames.Size = new Size(100, 23);
        btnExtractFrames.TabIndex = 8;
        btnExtractFrames.Text = "Extra Frames From Video";
        btnExtractFrames.UseVisualStyleBackColor = true;
        btnExtractFrames.Click += btnExtractFrames_Click;
        // 
        // statusStrip1
        // 
        statusStrip1.Location = new Point(0, 477);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(806, 22);
        statusStrip1.TabIndex = 9;
        statusStrip1.Text = "statusStrip1";
        // 
        // toolStrip1
        // 
        toolStrip1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        toolStrip1.Dock = DockStyle.None;
        toolStrip1.Items.AddRange(new ToolStripItem[] { lblStatus });
        toolStrip1.Location = new Point(0, 474);
        toolStrip1.Name = "toolStrip1";
        toolStrip1.Size = new Size(98, 25);
        toolStrip1.TabIndex = 10;
        toolStrip1.Text = "toolStrip1";
        // 
        // lblStatus
        // 
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(86, 22);
        lblStatus.Text = "toolStripLabel1";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(806, 499);
        Controls.Add(toolStrip1);
        Controls.Add(statusStrip1);
        Controls.Add(btnExtractFrames);
        Controls.Add(btnStill);
        Controls.Add(btnSettings);
        Controls.Add(btnViewLogs);
        Controls.Add(lblFrameTimestamp);
        Controls.Add(btnBrowseVideo);
        Controls.Add(pbVideoFeed);
        Controls.Add(btnStartStop);
        Controls.Add(cbWebcamDevices);
        Name = "Form1";
        Text = "SiteGuard PPE";
        Load += Form1_Load;
        ((System.ComponentModel.ISupportInitialize)pbVideoFeed).EndInit();
        toolStrip1.ResumeLayout(false);
        toolStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();

    }

    private System.Windows.Forms.ComboBox cbWebcamDevices;
    private System.Windows.Forms.Button btnStartStop;
    private System.Windows.Forms.PictureBox pbVideoFeed;
    private System.Windows.Forms.Button btnBrowseVideo;
    private System.Windows.Forms.Button btnStill; // New button declaration
    
    private System.Windows.Forms.OpenFileDialog openFileDialogStill; // New OpenFileDialog declaration
    private System.Windows.Forms.Label lblFrameTimestamp;
    private System.Windows.Forms.Button btnViewLogs;
    private System.Windows.Forms.Button btnSettings;

    #endregion


    private Button btnExtractFrames;
    private StatusStrip statusStrip1;
    private ToolStrip toolStrip1;
    private ToolStripLabel lblStatus;
}
