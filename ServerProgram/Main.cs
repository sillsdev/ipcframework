using System;
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
#if __MonoCS__
			get { return "/home/steve/Dropbox/IPCDev/ServerProgram/bin/Debug"; }
#else
			get { return "C:\\users\\mcconnel\\Dropbox\\IPCDev\\ServerProgram\\bin\\Debug"; }
#endif
		}
		public static string ProjectsDirectory
		{
#if __MonoCS__
			get { return "/home/steve/fwrepo/fw/DistFiles/Projects"; }
#else
			get { return "C:\\fwrepo\\fw\\DistFiles\\Projects"; }
#endif
		}
	}
}