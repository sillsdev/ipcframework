using System;
using System.ServiceModel;

namespace IPCFramework
{
	// ReSharper disable once InconsistentNaming
	internal class WindowsIPCHost : IIPCHost
	{
		private ServiceHost _host;

		#region Implement IIPCHost methods
		public bool Initialize<TClass, TInterface>(string connectionId, SimpleCallback alert, SimpleCallback cleanup)
		{
			try
			{
				// Increase timeouts over the default values of 1 minute, allow receives to take forever to allow us to open a call which detects when
				// the remote program crashes. This lets us lock one program out while the other end is processing.
				var hostPipeBinding = new NetNamedPipeBinding
				{
					ReceiveTimeout = TimeSpan.MaxValue, SendTimeout = TimeSpan.FromMinutes(3), OpenTimeout = TimeSpan.FromMinutes(3)
				};
				//open host ready for business
				_host = new ServiceHost(typeof(TClass), new Uri("net.pipe://localhost/" + connectionId));
				_host.AddServiceEndpoint(typeof(TInterface), hostPipeBinding, "FLExPipe");
				_host.Open();
			}
			catch (InvalidOperationException)
				// Can happen if Conflict Report is open and we try to run FLExBridge again.
			{
				((IDisposable) _host)?.Dispose();
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}
			catch (AddressAlreadyInUseException)
				// Can happen if FLExBridge has been launched and we try to launch FLExBridge again.
			{
				// host is normally not null for this exception, but there is no pipe to dispose
				_host = null;
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}
			return true;
		}

		public void Close()
		{
			try
			{
				if (_host != null)
				{
					_host.Close();
					((IDisposable)_host).Dispose();
				}
			}
			// ReSharper disable once EmptyGeneralCatchClause
			catch
			{
			}
			_host = null;
		}

		public int VerbosityLevel { get; set; }
		#endregion
	}
}