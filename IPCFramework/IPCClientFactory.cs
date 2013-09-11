using System;
using System.Text;
using System.Threading;
using System.Reflection;

#if __MonoCS__
using System.Net.Sockets;
using Mono.Unix;
#endif
using System.ServiceModel;

namespace IPCFramework
{
	public static class IPCClientFactory
	{
		public static IIPCClient Create()
		{
#if __MonoCS__
			return new UnixIPCClient();
#else
			return new WindowsIPCClient();
#endif
		}

#if __MonoCS__
		internal class UnixIPCClient : IIPCClient
		{
			object _waitObject;
			SimpleCallback _cleanup;
			Socket _sock;
			SimpleCallback _signalDone;
			string _endId;
			string _rpcMethod;

			/// <summary>
			/// This code is currently specific to the FLExBridge/FLEx usage of this library.
			/// It will return a pipe identifier which will fit under the UNIX limit for a
			/// named UNIX socket.
			/// </summary>
			/// <returns>The end identifier.</returns>
			/// <param name="connectionId">Connection identifier.</param>
			string ComputeEndId(string connectionId)
			{
				StringBuilder sb = new StringBuilder();
				if (connectionId.StartsWith("FLExBridgeEndpoint"))
					sb.Append("Bridge-");
				else
					sb.Append("FLEx-");
				int idxBegin = connectionId.IndexOf(".fwdata");
				int idxEnd;
				if (idxBegin > 0)
				{
					idxBegin += 7;
					idxEnd = connectionId.IndexOf ("_", idxBegin);
				}
				else
				{
					idxBegin = connectionId.LastIndexOf('/');
					idxEnd = connectionId.Length - 1;
				}
				sb.Append(connectionId.Substring(idxBegin, idxEnd - idxBegin));
				return sb.ToString();
			}

			#region Implement IIPCClient methods
			public bool Initialize<TInterface>(string connectionId, object waitObject, SimpleCallback cleanup)
			{
				try
				{
					var id = IPCHostFactory.UnixIPCHost.TruncateId(connectionId);
					_endId = ComputeEndId(connectionId);
					_waitObject = waitObject;
					_cleanup = cleanup;
					_sock = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].Initialize() - connecting to {1}", _endId, id);
					var endPoint = new AbstractUnixEndPoint(id);
					_sock.Connect(endPoint);
					return true;
				}
				catch (Exception e)
				{
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].Initialize() - caught exception: {1}", _endId ?? connectionId, e.Message);
					return false;
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
					_rpcMethod = rpcMethod;
					_signalDone = null;
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].RemoteCall(\"{1}\", ...) - calling _sock.Send()", _endId, rpcMethod);
					var bytes = CreateSendData(rpcMethod, args);
					_sock.Send(bytes, 0, bytes.Length, SocketFlags.None);
					GetReturnValue();
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
			
			public bool RemoteCall(string rpcMethod, SimpleCallback signalDone)
			{
				return RemoteCall(rpcMethod, null, signalDone);
			}
			
			public bool RemoteCall(string rpcMethod, object[] args, SimpleCallback signalDone)
			{
				try
				{
					_rpcMethod = rpcMethod;
					_signalDone = signalDone;
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].RemoteCall(\"{1}\", ..., signalDone) - calling _sock.BeginSend(..., SendCallback, ...)",
							_endId, rpcMethod);
					var bytes = CreateSendData(rpcMethod, args);
					_sock.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, _sock);
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
			
			public void Close()
			{
				try
				{
					if (_cleanup != null)
					{
						_cleanup();
						_cleanup = null;
					}
					if (_sock != null)
					{
						_sock.Close();
						_sock = null;
					}
				}
				catch
				{
				}
			}
			
			public int VerbosityLevel { get; set; }
			#endregion

			/// <summary>
			/// Translate the name of the remote method and any arguments to a properly
			/// formatted array of bytes for transmission to the far end of the connection.
			/// </summary>
			/// <param name="rpcMethod">name of the remote method</param>
			/// <param name="args">array of arguments.  This may be null or empty.</param>
			/// <returns>an array of bytes encoding the method call</returns>
			byte[] CreateSendData (string rpcMethod, object[] args)
			{
				StringBuilder bldr = new StringBuilder();
				bldr.Append(rpcMethod);
				if (args != null && args.Length > 0)
				{
					for (int i = 0; i < args.Length; ++i)
						bldr.AppendFormat("\n{0}", args[i]);
				}
				bldr.Append("\n<EOF>");
				return Encoding.UTF8.GetBytes(bldr.ToString());
			}

			private class ReturnValueHelp
			{
				public string MethodName { get; private set; }
				public byte[] Buffer { get; private set; }
				public StringBuilder Bldr { get; private set; }
				public Socket Socket { get; private set; }
				public SimpleCallback SignalDone { get; private set; }
				public string EndId { get; private set; }

				public ReturnValueHelp(string method, Socket socket, SimpleCallback signalDone, string endId)
				{
					MethodName = method;
					Buffer = new byte[1024];
					Bldr = new StringBuilder();
					Socket = socket;
					SignalDone = signalDone;
					EndId = endId;
				}
			}

			/// <summary>
			/// Wait to receive acknowledgement from the remote end.
			/// </summary>
			/// <returns>string value of the return packet</returns>
			void GetReturnValue()
			{
				try
				{
					if (VerbosityLevel >= 2)
						Console.WriteLine("IPCHost[{0}].GetReturnValue() for {1} - calling _sock.BeginReceive(..., ReceiveCallback, ...)",
							_endId, _rpcMethod);
					var help = new ReturnValueHelp(_rpcMethod, _sock, _signalDone, _endId);
					_sock.BeginReceive(help.Buffer, 0, help.Buffer.Length, SocketFlags.None,  ReceiveCallback, help);
				}
				catch (Exception e)
				{
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].GetReturnValue() for {1} - caught exception: {2}",
							_endId, _rpcMethod, e.Message);
				}
			}

			private void ReceiveCallback(IAsyncResult iar)
			{
				var help = (ReturnValueHelp)iar.AsyncState;
				try
				{
					var len = help.Socket.EndReceive(iar);
					var retval = Encoding.UTF8.GetString(help.Buffer, 0, len);
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].ReceiveCallback() for {1} - _sock.EndReceive() => \"{2}\"",
							help.EndId, help.MethodName, retval);
					if (help.SignalDone != null)
						help.SignalDone();
					if (_cleanup != null)
						_cleanup();
				}
				catch (Exception e)
				{
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].ReceiveCallback() for {1} - caught exception: {2}",
							help.EndId, help.MethodName, e.Message);
				}
			}

			/// <summary>
			/// This callback gets called fairly quickly, possibly as soon as the separate
			/// thread has been completed for the asynchronous Socket.BeginSend.  Finish
			/// the send, and then start an asynchronous receive to get the reply.
			/// </summary>
			private void SendCallback(IAsyncResult iar)
			{
				Monitor.Enter(_waitObject);
				try
				{
					var sock = (Socket)iar.AsyncState;
					sock.EndSend(iar);
					if (VerbosityLevel >= 2)
						Console.WriteLine("IPCClient[{0}].SendCallback() for {1} - _sock.EndSend() finished",
							_endId, _rpcMethod);
					GetReturnValue();
				}
				catch (SocketException)
				{
					//Something went wrong with the communication to the Bridge. Possibly it finished/died already, wake up the host program
					if (_signalDone != null)
						_signalDone();
					Monitor.Pulse(_waitObject);
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].SendCallback() for {1} - caught SocketException and woke up Host (?)",
							_endId, _rpcMethod);
					if (_cleanup != null)
						_cleanup();
				}
				catch (Exception e)
				{
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCClient[{0}].SendCallback() for {1} - caught Exception: {2}",
							_endId, _rpcMethod, e.Message);
					if (_cleanup != null)
						_cleanup();
				}
				finally
				{
					// Do NOT call cleanup here, we don't want to cleanup until we either get the result or fail.
					Monitor.Exit(_waitObject);
				}
			}
		}
#else
		private class WindowsIPCClient : IIPCClient
		{
			Type _clientType;
			object _waitObject;
			SimpleCallback _cleanup;
			SimpleCallback _signalDone;
			string _rpcMethod;
			object _channel;

			#region Implement IIPCClient methods
			public bool Initialize<TInterface>(string connectionId, object waitObject, SimpleCallback cleanup)
			{
				try
				{
					_clientType = typeof(TInterface);
					_waitObject = waitObject;
					_cleanup = cleanup;
					var clientPipeBinding = new NetNamedPipeBinding {ReceiveTimeout = TimeSpan.MaxValue};
					var factory = new ChannelFactory<TInterface>(clientPipeBinding,
						new EndpointAddress("net.pipe://localhost/" + connectionId + "/FLExPipe"));
					_channel = factory.CreateChannel();

					var pi = _channel as IServiceChannel;
					if (pi != null)
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
						mi.Invoke(_channel, args);
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
						mi.Invoke(_channel, new object[] { (AsyncCallback)WorkDoneCallback, _channel });
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
				if (_cleanup != null)
					_cleanup();
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
					if (_signalDone != null)
						_signalDone();
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
				catch (Exception)
				{
				}
				finally
				{
					if (_cleanup != null)
						_cleanup();
					Monitor.Exit(_waitObject);
				}
			}
		}
#endif
	}
}

