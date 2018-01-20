namespace RXPatchLib.AXDebug
{
    partial class FrmAgnDebug
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
            this.lstDownloads = new System.Windows.Forms.ListView();
            this.chGUID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chServerUrl = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chFilePath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chProgress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // lstDownloads
            // 
            this.lstDownloads.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstDownloads.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chGUID,
            this.chServerUrl,
            this.chFilePath,
            this.chStatus,
            this.chProgress});
            this.lstDownloads.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstDownloads.Location = new System.Drawing.Point(12, 12);
            this.lstDownloads.Name = "lstDownloads";
            this.lstDownloads.Size = new System.Drawing.Size(1539, 522);
            this.lstDownloads.TabIndex = 0;
            this.lstDownloads.UseCompatibleStateImageBehavior = false;
            this.lstDownloads.View = System.Windows.Forms.View.Details;
            // 
            // chGUID
            // 
            this.chGUID.Text = "GUID";
            this.chGUID.Width = 213;
            // 
            // chServerUrl
            // 
            this.chServerUrl.Text = "Server URL";
            this.chServerUrl.Width = 256;
            // 
            // chFilePath
            // 
            this.chFilePath.Text = "File Path";
            this.chFilePath.Width = 505;
            // 
            // chStatus
            // 
            this.chStatus.Text = "Status";
            this.chStatus.Width = 215;
            // 
            // chProgress
            // 
            this.chProgress.Text = "Progress";
            this.chProgress.Width = 248;
            // 
            // FrmAgnDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1563, 546);
            this.ControlBox = false;
            this.Controls.Add(this.lstDownloads);
            this.Name = "FrmAgnDebug";
            this.Text = "Concurrent Downloading Debug";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView lstDownloads;
        private System.Windows.Forms.ColumnHeader chGUID;
        private System.Windows.Forms.ColumnHeader chServerUrl;
        private System.Windows.Forms.ColumnHeader chFilePath;
        private System.Windows.Forms.ColumnHeader chStatus;
        private System.Windows.Forms.ColumnHeader chProgress;
    }
}