using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace ServerProgram
{
	public partial class ServerForm : Form
	{
		string _project;

		public ServerForm()
		{
			InitializeComponent();
			_project = Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, "Test"), "Test.fwdata");
		}

		private void _btnAbout_Click(object sender, EventArgs e)
		{
			_label.Text = "processing AboutFLExBridge";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
			bool changesReceived;
			string projectName;
			bool fOK = FLExBridgeHelper.LaunchFieldworksBridge(_project, "steve", FLExBridgeHelper.AboutFLExBridge,
				null, 7000066, "0.13", out changesReceived, out projectName);
			_label.Text = String.Format("AboutFLExBridge: changesReceived = {0}, projectName = \"{1}\", fOK = {2}", changesReceived, projectName, fOK);
			Console.WriteLine(_label.Text);
		}

		private void _btnSendReceive_Click(object sender, EventArgs e)
		{
			_label.Text = "processing SendReceive";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
			bool changesReceived;
			string projectName;
			bool fOK = FLExBridgeHelper.LaunchFieldworksBridge(_project, "steve", FLExBridgeHelper.SendReceive,
				null, 7000066, "0.13", out changesReceived, out projectName);
			_label.Text = String.Format("SendReceive: changesReceived = {0}, projectName = \"{1}\", fOK = {2}", changesReceived, projectName, fOK);
			Console.WriteLine(_label.Text);
		}

		private void _btnViewMsgs_Click(object sender, EventArgs e)
		{
			_label.Text = "ViewMsgs: no test implemented";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
		}

		private void _btnLexiconSR_Click(object sender, EventArgs e)
		{
			_label.Text = "LexiconSR: no test implemented";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
		}

		private void _btnViewLexiconMsgs_Click(object sender, EventArgs e)
		{
			_label.Text = "ViewLexiconMsgs: no test implemented";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
		}

		private void _btnGetProject_Click(object sender, EventArgs e)
		{
			_label.Text = "GetProject: no test implemented";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
		}

		private void _btnGetLexicon_Click(object sender, EventArgs e)
		{
			_label.Text = "GetLexicon: no test implemented";
			Console.WriteLine();
			Console.WriteLine(_label.Text);
		}
	}
}
