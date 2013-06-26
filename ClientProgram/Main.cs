using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace ClientProgram
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			//System.Windows.Forms.MessageBox.Show("You can attach to ClientProgram now.", "ClientProgram");
			Console.WriteLine("ClientProgram started");
			var options = CommandLineProcessor.ParseCommandLineArgs(args);
			var connHelper = new FLExConnectionHelper();
			if (!connHelper.Init(options))
				return;
			var action = options["-v"];
			switch (action)
			{
			case "about_flex_bridge":
			case "view_notes":
			case "view_notes_lift":
			case "check_for_updates":
				break;
			case "send_receive":
				for (int i = 0; i < 15; ++i)
				{
					Thread.Sleep(1000);
					Console.Write("S/R...");
				}
				Console.WriteLine();
				connHelper.SignalBridgeWorkComplete(true);
				break;
			case "send_receive_lift":
				for (int i = 0; i < 10; ++i)
				{
					Thread.Sleep(1000);
					Console.Write("S/RL...");
				}
				Console.WriteLine();
				break;
			case "obtain":
				break;
			case "obtain_lift":
				break;
			case "undo_export_lift":
				break;
			case "move_lift":
				break;
			}
			Console.WriteLine("ClientProgram about to call connHelper.Dispose() on its way out the door...");
			connHelper.Dispose();
		}
	}

	public interface ICreateProjectFromLift
	{
		bool CreateProjectFromLift(string liftPath);
	}

	public class JumpEventArgs : EventArgs
	{
		private readonly string _jumpUrl;
		
		public JumpEventArgs(string jumpUrl)
		{
			_jumpUrl = jumpUrl;
		}
		
		public string JumpUrl
		{
			get { return _jumpUrl; }
		}
	}

	public static class Utilities
	{
		public const string FwXmlExtension = "." + FwXmlExtensionNoPeriod;
		public const string FwXmlExtensionNoPeriod = "fwdata";
		public const string FwDb4oExtension = "." + FwDb4oExtensionNoPeriod;
		public const string FwDb4oExtensionNoPeriod = "fwdb";
		public const string OtherRepositories = "OtherRepositories";
		public const string LIFT = "LIFT";
		public const string hg = ".hg";

		public static string LiftOffset(string path)
		{
			var otherPath = Path.Combine(path, OtherRepositories);
			if (Directory.Exists(otherPath))
			{
				var extantLiftFolder = Directory.GetDirectories(otherPath).FirstOrDefault(subfolder => subfolder.EndsWith("_LIFT"));
				if (extantLiftFolder != null)
					return extantLiftFolder;
			}
			return Path.Combine(path, OtherRepositories, Path.GetFileName(path) + "_" + LIFT);
		}
	}
}
