using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ServerProgram
{
	class MainClass
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new ServerForm());
		}
	}

	public static class FileUtils
	{
		public static string StripFilePrefix(string fileString)
		{
			if (fileString == null)
				return fileString;
			string prefix = Uri.UriSchemeFile + ":";
			if (!fileString.StartsWith(prefix))
				return fileString;
			string path = fileString.Substring(prefix.Length);
			// Trim any number of beginning slashes
			path = path.TrimStart('/');
			// Prepend slash on Linux
			if (Environment.OSVersion.Platform == PlatformID.Unix)
				path = '/' + path;
			return path;
		}
	}

	public static class DirectoryFinder
	{
		public static string FlexBridgeFolder
		{
			get
			{
				var p = FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase);
				return Path.GetDirectoryName(p);
			}
		}
		public static string ProjectsDirectory
		{
#if MONO
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "fwrepo/fw/DistFiles/Projects"); }
#else
			get { return "C:\\fwrepo\\fw\\DistFiles\\Projects"; }
#endif
		}
	}
}
