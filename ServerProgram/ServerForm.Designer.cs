namespace ServerProgram
{
	partial class ServerForm
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
			this._btnAbout = new System.Windows.Forms.Button();
			this._btnSendReceive = new System.Windows.Forms.Button();
			this._btnViewMsgs = new System.Windows.Forms.Button();
			this._btnLexiconSR = new System.Windows.Forms.Button();
			this._btnViewLexiconMsgs = new System.Windows.Forms.Button();
			this._btnGetProject = new System.Windows.Forms.Button();
			this._btnGetLexicon = new System.Windows.Forms.Button();
			this._label = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _btnAbout
			// 
			this._btnAbout.Location = new System.Drawing.Point(10, 10);
			this._btnAbout.Name = "_btnAbout";
			this._btnAbout.Size = new System.Drawing.Size(200, 23);
			this._btnAbout.TabIndex = 0;
			this._btnAbout.Text = "About ...";
			this._btnAbout.UseVisualStyleBackColor = true;
			this._btnAbout.Click += new System.EventHandler(this._btnAbout_Click);
			// 
			// _btnSendReceive
			// 
			this._btnSendReceive.Location = new System.Drawing.Point(10, 45);
			this._btnSendReceive.Name = "_btnSendReceive";
			this._btnSendReceive.Size = new System.Drawing.Size(200, 23);
			this._btnSendReceive.TabIndex = 1;
			this._btnSendReceive.Text = "Project Send/Receive";
			this._btnSendReceive.UseVisualStyleBackColor = true;
			this._btnSendReceive.Click += new System.EventHandler(this._btnSendReceive_Click);
			// 
			// _btnViewMsgs
			// 
			this._btnViewMsgs.Location = new System.Drawing.Point(10, 80);
			this._btnViewMsgs.Name = "_btnViewMsgs";
			this._btnViewMsgs.Size = new System.Drawing.Size(200, 23);
			this._btnViewMsgs.TabIndex = 2;
			this._btnViewMsgs.Text = "View Project Messages";
			this._btnViewMsgs.UseVisualStyleBackColor = true;
			this._btnViewMsgs.Click += new System.EventHandler(this._btnViewMsgs_Click);
			// 
			// _btnLexiconSR
			// 
			this._btnLexiconSR.Location = new System.Drawing.Point(10, 115);
			this._btnLexiconSR.Name = "_btnLexiconSR";
			this._btnLexiconSR.Size = new System.Drawing.Size(200, 23);
			this._btnLexiconSR.TabIndex = 3;
			this._btnLexiconSR.Text = "Lexicon Send/Receive (LIFT)";
			this._btnLexiconSR.UseVisualStyleBackColor = true;
			this._btnLexiconSR.Click += new System.EventHandler(this._btnLexiconSR_Click);
			// 
			// _btnViewLexiconMsgs
			// 
			this._btnViewLexiconMsgs.Location = new System.Drawing.Point(10, 150);
			this._btnViewLexiconMsgs.Name = "_btnViewLexiconMsgs";
			this._btnViewLexiconMsgs.Size = new System.Drawing.Size(200, 23);
			this._btnViewLexiconMsgs.TabIndex = 4;
			this._btnViewLexiconMsgs.Text = "View Lexicon Messages";
			this._btnViewLexiconMsgs.UseVisualStyleBackColor = true;
			this._btnViewLexiconMsgs.Click += new System.EventHandler(this._btnViewLexiconMsgs_Click);
			// 
			// _btnGetProject
			// 
			this._btnGetProject.Location = new System.Drawing.Point(10, 185);
			this._btnGetProject.Name = "_btnGetProject";
			this._btnGetProject.Size = new System.Drawing.Size(200, 23);
			this._btnGetProject.TabIndex = 5;
			this._btnGetProject.Text = "Get Project from Colleagues ...";
			this._btnGetProject.UseVisualStyleBackColor = true;
			this._btnGetProject.Click += new System.EventHandler(this._btnGetProject_Click);
			// 
			// _btnGetLexicon
			// 
			this._btnGetLexicon.Location = new System.Drawing.Point(10, 220);
			this._btnGetLexicon.Name = "_btnGetLexicon";
			this._btnGetLexicon.Size = new System.Drawing.Size(200, 23);
			this._btnGetLexicon.TabIndex = 6;
			this._btnGetLexicon.Text = "Get and Merge Lexicon ...";
			this._btnGetLexicon.UseVisualStyleBackColor = true;
			this._btnGetLexicon.Click += new System.EventHandler(this._btnGetLexicon_Click);
			// 
			// _label
			// 
			this._label.AutoSize = true;
			this._label.Location = new System.Drawing.Point(10, 260);
			this._label.Name = "_label";
			this._label.Size = new System.Drawing.Size(24, 13);
			this._label.TabIndex = 7;
			this._label.Text = "Idle";
			// 
			// ServerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 282);
			this.Controls.Add(this._label);
			this.Controls.Add(this._btnGetLexicon);
			this.Controls.Add(this._btnGetProject);
			this.Controls.Add(this._btnViewLexiconMsgs);
			this.Controls.Add(this._btnLexiconSR);
			this.Controls.Add(this._btnViewMsgs);
			this.Controls.Add(this._btnSendReceive);
			this.Controls.Add(this._btnAbout);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(300, 320);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(300, 320);
			this.Name = "ServerForm";
			this.Text = "Test IPC Code";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _btnAbout;
		private System.Windows.Forms.Button _btnSendReceive;
		private System.Windows.Forms.Button _btnViewMsgs;
		private System.Windows.Forms.Button _btnLexiconSR;
		private System.Windows.Forms.Button _btnViewLexiconMsgs;
		private System.Windows.Forms.Button _btnGetProject;
		private System.Windows.Forms.Button _btnGetLexicon;
		private System.Windows.Forms.Label _label;
	}
}

