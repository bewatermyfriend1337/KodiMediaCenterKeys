namespace Kodi_Media_Keys
{
    partial class frmKodiMediaKeys
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmKodiMediaKeys));
            this.niKodi_MediaKeys = new System.Windows.Forms.NotifyIcon(this.components);
            this.pbLCD = new System.Windows.Forms.PictureBox();
            this.cmsKodi_MediaKeys = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pbLCD)).BeginInit();
            this.cmsKodi_MediaKeys.SuspendLayout();
            this.SuspendLayout();
            // 
            // niKodi_MediaKeys
            // 
            this.niKodi_MediaKeys.ContextMenuStrip = this.cmsKodi_MediaKeys;
            this.niKodi_MediaKeys.Icon = ((System.Drawing.Icon)(resources.GetObject("niKodi_MediaKeys.Icon")));
            this.niKodi_MediaKeys.Text = "Kodi Media Keys";
            this.niKodi_MediaKeys.Visible = true;
            this.niKodi_MediaKeys.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.niKodi_MediaKeys_MouseDoubleClick);
            // 
            // pbLCD
            // 
            this.pbLCD.Location = new System.Drawing.Point(13, 13);
            this.pbLCD.Name = "pbLCD";
            this.pbLCD.Size = new System.Drawing.Size(160, 43);
            this.pbLCD.TabIndex = 0;
            this.pbLCD.TabStop = false;
            // 
            // cmsKodi_MediaKeys
            // 
            this.cmsKodi_MediaKeys.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.cmsKodi_MediaKeys.Name = "cmsKodi_MediaKeys";
            this.cmsKodi_MediaKeys.Size = new System.Drawing.Size(93, 26);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // frmKodiMediaKeys
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.pbLCD);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmKodiMediaKeys";
            this.ShowInTaskbar = false;
            this.Text = "Kodi Media Keys";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmKodiMediaKeys_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pbLCD)).EndInit();
            this.cmsKodi_MediaKeys.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon niKodi_MediaKeys;
        private System.Windows.Forms.PictureBox pbLCD;
        private System.Windows.Forms.ContextMenuStrip cmsKodi_MediaKeys;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
    }
}

