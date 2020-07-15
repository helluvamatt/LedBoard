namespace LedBoard.Screensaver
{
	partial class ConfigForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.groupGif = new System.Windows.Forms.GroupBox();
			this.fldPath = new System.Windows.Forms.Label();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.lblPath = new System.Windows.Forms.Label();
			this.btnLaunchApp = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.groupGif.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.OnCancelClick);
			// 
			// btnOk
			// 
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.OnOkClick);
			// 
			// groupGif
			// 
			resources.ApplyResources(this.groupGif, "groupGif");
			this.groupGif.Controls.Add(this.fldPath);
			this.groupGif.Controls.Add(this.btnBrowse);
			this.groupGif.Controls.Add(this.lblPath);
			this.groupGif.Name = "groupGif";
			this.groupGif.TabStop = false;
			// 
			// fldPath
			// 
			resources.ApplyResources(this.fldPath, "fldPath");
			this.fldPath.DataBindings.Add(new System.Windows.Forms.Binding("Text", this, "ImagePath", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.fldPath.Name = "fldPath";
			// 
			// btnBrowse
			// 
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.OnBrowseClick);
			// 
			// lblPath
			// 
			resources.ApplyResources(this.lblPath, "lblPath");
			this.lblPath.Name = "lblPath";
			// 
			// btnLaunchApp
			// 
			resources.ApplyResources(this.btnLaunchApp, "btnLaunchApp");
			this.btnLaunchApp.Image = global::LedBoard.Screensaver.Properties.Resources.app_img;
			this.btnLaunchApp.Name = "btnLaunchApp";
			this.btnLaunchApp.UseVisualStyleBackColor = true;
			this.btnLaunchApp.Click += new System.EventHandler(this.OnOpenAppClick);
			// 
			// openFileDialog
			// 
			resources.ApplyResources(this.openFileDialog, "openFileDialog");
			// 
			// ConfigForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.btnLaunchApp);
			this.Controls.Add(this.groupGif);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnCancel);
			this.Icon = global::LedBoard.Screensaver.Properties.Resources.app;
			this.MaximizeBox = false;
			this.Name = "ConfigForm";
			this.groupGif.ResumeLayout(false);
			this.groupGif.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupGif;
		private System.Windows.Forms.Button btnLaunchApp;
		private System.Windows.Forms.Label fldPath;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.Label lblPath;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
	}
}

