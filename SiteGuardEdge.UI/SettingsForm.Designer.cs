namespace SiteGuardEdge.UI;

partial class SettingsForm
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
        this.lblLogRetention = new System.Windows.Forms.Label();
        this.cbLogRetention = new System.Windows.Forms.ComboBox();
        this.btnSaveSettings = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // lblLogRetention
        // 
        this.lblLogRetention.AutoSize = true;
        this.lblLogRetention.Location = new System.Drawing.Point(12, 15);
        this.lblLogRetention.Name = "lblLogRetention";
        this.lblLogRetention.Size = new System.Drawing.Size(114, 15);
        this.lblLogRetention.TabIndex = 0;
        this.lblLogRetention.Text = "Log Retention Period:";
        // 
        // cbLogRetention
        // 
        this.cbLogRetention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cbLogRetention.FormattingEnabled = true;
        this.cbLogRetention.Location = new System.Drawing.Point(132, 12);
        this.cbLogRetention.Name = "cbLogRetention";
        this.cbLogRetention.Size = new System.Drawing.Size(150, 23);
        this.cbLogRetention.TabIndex = 1;
        // 
        // btnSaveSettings
        // 
        this.btnSaveSettings.Location = new System.Drawing.Point(12, 50);
        this.btnSaveSettings.Name = "btnSaveSettings";
        this.btnSaveSettings.Size = new System.Drawing.Size(100, 23);
        this.btnSaveSettings.TabIndex = 2;
        this.btnSaveSettings.Text = "Save Settings";
        this.btnSaveSettings.UseVisualStyleBackColor = true;
        this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
        // 
        // SettingsForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(300, 100);
        this.Controls.Add(this.btnSaveSettings);
        this.Controls.Add(this.cbLogRetention);
        this.Controls.Add(this.lblLogRetention);
        this.Name = "SettingsForm";
        this.Text = "Settings";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    private System.Windows.Forms.Label lblLogRetention;
    private System.Windows.Forms.ComboBox cbLogRetention;
    private System.Windows.Forms.Button btnSaveSettings;

    #endregion
}