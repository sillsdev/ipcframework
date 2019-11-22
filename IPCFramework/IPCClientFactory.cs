using SIL.PlatformUtilities;

namespace IPCFramework
{
	// ReSharper disable once InconsistentNaming
	public static class IPCClientFactory
	{
		public static IIPCClient Create()
		{
			if (Platform.IsLinux)
				return new UnixIPCClient();
			return new WindowsIPCClient();
		}
	}
}

