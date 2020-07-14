namespace LedBoard.Screensaver
{
	partial class ScreensaverForm
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
			this.pb = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pb)).BeginInit();
			this.SuspendLayout();
			// 
			// pb
			// 
			this.pb.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pb.Location = new System.Drawing.Point(0, 0);
			this.pb.Name = "pb";
			this.pb.Size = new System.Drawing.Size(800, 450);
			this.pb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pb.TabIndex = 0;
			this.pb.TabStop = false;
			this.pb.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnMouseClick);
			this.pb.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
			// 
			// ScreensaverForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.pb);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = global::LedBoard.Screensaver.Properties.Resources.app;
			this.Name = "ScreensaverForm";
			this.Text = "LedBoard Screensaver";
			this.TopMost = true;
			((System.ComponentModel.ISupportInitialize)(this.pb)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pb;
	}
}