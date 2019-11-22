using System;
using System.Reflection;
using System.ServiceModel;
using System.Threading;

namespace IPCFramework
{
	internal class WindowsIPCClient : IIPCClient
	{
		Type           _clientType;
		object         _waitObject;
		SimpleCallback _cleanup;
		SimpleCallback _signalDone;
		string         _rpcMethod;
		object         _channel;

		#region Implement IIPCClient methods
		public bool Initialize<TInterface>(string connectionId, object waitObject, SimpleCallback cleanup)
		{
			try
			{
				_clientType = typeof(TInterface);
				_waitObject = waitObject;
				_cleanup = cleanup;
				// Increase timeouts over the default values of 1 minute, allow receives to take forever to allow us to open a call which detects when
				// the remote program crashes. This lets us lock one program out while the other end is processing.
				var clientPipeBinding = new NetNamedPipeBinding
				{
					ReceiveTimeout = TimeSpan.MaxValue, OpenTimeout = TimeSpan.FromMinutes(3), SendTimeout = TimeSpan.FromMinutes(3)
				};
				var factory = new ChannelFactory<TInterface>(clientPipeBinding,
					new EndpointAddress("net.pipe://localhost/" + connectionId + "/FLExPipe"));
				_channel = factory.CreateChannel();

				if (_channel is IServiceChannel pi)
					pi.OperationTimeout = TimeSpan.MaxValue;
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("WindowsIPCClient.Initialize(\"{0}\") caught exception: {1}",
					connectionId, ex.Message);
				throw;
			}
		}

		public bool RemoteCall(string rpcMethod)
		{
			return RemoteCall(rpcMethod, new object[0]);
		}

		public bool RemoteCall(string rpcMethod, object[] args)
		{
			try
			{
				var mi = _clientType.GetMethod(rpcMethod);
				if (mi != null)
				{
					try
					{
						mi.Invoke(_channel, args);
					}
					catch (TargetInvocationException e)
					{
						// A dropped connection means we cannot call the other end. Return false, but don't throw.
						if (e.InnerException is EndpointNotFoundException)
							return false;
						throw; //other reasons for failing are exceptional
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("WindowsIPCClient.RemoteCall(\"{0}\") caught exception: {1}",
					rpcMethod, ex.Message);
				if (rpcMethod == "BridgeWorkComplete")
					return false;
				throw;
			}
		}

		public bool RemoteCall(string rpcMethod, SimpleCallback signalDone)
		{
			try
			{
				_signalDone = signalDone;
				_rpcMethod = rpcMethod;
				var mi = _clientType.GetMethod("Begin" + rpcMethod);
				if (mi != null)
					mi.Invoke(_channel, new[] { (AsyncCallback)WorkDoneCallback, _channel });
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("WindowsIPCClient.RemoteCall(\"{0}\") caught exception: {1}",
					rpcMethod, ex.Message);
				throw;
			}
		}

		public bool RemoteCall(string rpcMethod, object[] args, SimpleCallback signalDone)
		{
			// I'm not sure how to implement this!
			return false;
		}

		public void Close()
		{
			_cleanup?.Invoke();
		}

		public int VerbosityLevel { get; set; }
		#endregion

		/// <summary>
		/// This callback mostly serves to help us terminate in exceptional cases.
		/// It is not reliable for return data because it is asynchronous, and FLExBridge might close before we retrieve the data
		/// </summary>
		/// <param name="iar"></param>
		private void WorkDoneCallback(IAsyncResult iar)
		{
			Monitor.Enter(_waitObject);
			try
			{
				_signalDone?.Invoke();
				Monitor.Pulse(_waitObject);
				var mi = _clientType.GetMethod("End" + _rpcMethod);
				//((TInterface)iar.AsyncState).EndBridgeWorkOngoing(iar);
				if (mi != null)
					mi.Invoke(iar.AsyncState, new object[] { iar });
			}
			catch(CommunicationException)
			{
				//Something went wrong with the communication to the Bridge. Possibly it died unexpectedly, wake up FLEx
				Monitor.Pulse(_waitObject);
			}
			// ReSharper disable once EmptyGeneralCatchClause
			catch (Exception)
			{
			}
			finally
			{
				_cleanup?.Invoke();
				Monitor.Exit(_waitObject);
			}
		}
	}
}