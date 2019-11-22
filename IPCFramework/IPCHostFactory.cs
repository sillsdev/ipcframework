using SIL.PlatformUtilities;

namespace IPCFramework
{
	/// <summary>
	/// Factory for creating a system-specific IIPCHost implementation object.
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public static class IPCHostFactory
	{
		public static IIPCHost Create()
		{
			if (Platform.IsLinux)
				return new UnixIPCHost();
			return new WindowsIPCHost();
		}
	}
}
