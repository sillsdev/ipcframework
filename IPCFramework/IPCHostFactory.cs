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
	/// <summary>
	/// Tags a method in a class as one that finishes the current task that
	/// uses interprocess communication.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class FinishServerTask : Attribute
	{
		public override string ToString()
		{
			return "Finished!";
		}
	}

	/// <summary>
	/// Factory for creating a system-specific IIPCHost implementation object.
	/// </summary>
	public static class IPCHostFactory
	{
		public static IIPCHost Create()
		{
#if __MonoCS__
			return new UnixIPCHost();
#else
			return new WindowsIPCHost();
#endif
		}

#if __MonoCS__
		private class UnixIPCHost : IIPCHost
		{
			Socket _host;
			Type _serviceClass;
			SimpleCallback _alert;
			SimpleCallback _cleanup;

			string _endId;

			~UnixIPCHost()
			{
				Close();
			}

			string ComputeEndId (string connectionId)
			{
				StringBuilder sb = new StringBuilder();
				if (connectionId.StartsWith("FLExBridgeEndpoint"))
					sb.Append("Bridge-");
				else
					sb.Append("FLEx-");
				int idxBegin = connectionId.IndexOf(".fwdata");
				idxBegin += 7;
				int idxEnd = connectionId.IndexOf("_", idxBegin);
				sb.Append(connectionId.Substring(idxBegin, idxEnd - idxBegin));
				return sb.ToString();
			}

			#region Implement IIPCHost methods
			public bool Initialize<TClass,TInterface>(string connectionId, SimpleCallback alert, SimpleCallback cleanup)
			{
				_endId = ComputeEndId(connectionId);
				_host = null;
				_serviceClass = typeof(TClass);
				_alert = alert;
				_cleanup = cleanup;
				try
				{
					_host = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCHost[{0}].Initialize(): binding to {1}", _endId, connectionId);
					var endPoint = new AbstractUnixEndPoint(connectionId);
					_host.Bind(endPoint);
					_host.Listen(5);
					_host.BeginAccept(new AsyncCallback(SocketAcceptCallback), _host);
				}
				catch (Exception ex)
				{
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCHost[{0}].Initialize - caught exception: {1}", _endId, ex.Message);
					if (_host != null)
					{
						_host.Close();
						_host = null;
					}
					return false;
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
						_host = null;
					}
				}
				catch (Exception)
				{
					_host = null;
				}
			}

			public int VerbosityLevel { get; set; }
			#endregion

			/// <summary>
			/// This callback operates on its own thread.  After finishing the Accept operation,
			/// start an asynchronous BeginReceive operation on yet another thread.  After
			/// that, start checking for errors on the socket once a second (for a blocking host).
			/// </summary>
			/// <param name="ar">Ar.</param>
			void SocketAcceptCallback(IAsyncResult ar)
			{
				// Get the socket that handles the client request.
				var host = (Socket)ar.AsyncState;
				var handler = host.EndAccept(ar);
				// Create the state object.
				var state = new UnixIPCState(_serviceClass);
				state.WorkSocket = handler;
				if (VerbosityLevel >= 2)
					Console.WriteLine("IPCHost[{0}].SocketAcceptCallback() - calling handler.BeginReceive(..., HostReceiveCallback, ...)", _endId);
				handler.BeginReceive(state.Buffer, 0, UnixIPCState.BufferSize, 0,
					new AsyncCallback(HostReceiveCallback), state);
				while (_host != null)
				{
					if (handler.Poll(10000, SelectMode.SelectError))
					{
						if (VerbosityLevel >= 2)
							Console.WriteLine("IPCHost[{0}].SocketAcceptCallback() - calling alert and cleanup delegates", _endId);
						if (_alert != null)
							_alert();
						if (_cleanup != null)
							_cleanup();
						break;
					}
					Thread.Sleep(1000);
				}
			}

			void HostReceiveCallback(IAsyncResult ar)
			{
				if (VerbosityLevel >= 2)
					Console.WriteLine("IPCHost[{0}].HostReceiveCallback()", _endId);
				bool done = false;
				var state = (UnixIPCState)ar.AsyncState;
				var handler = state.WorkSocket;
				try
				{
					// Read data from the client socket.
					int bytesRead = handler.EndReceive(ar);
					if (VerbosityLevel >= 2)
						Console.WriteLine("IPCHost[{0}].HostReceiveCallback() - handler.EndReceive() read {1} bytes", _endId, bytesRead);
					if (bytesRead > 0)
					{
						// There  might be more data, so store the data received so far.
						state.Bldr.Append(Encoding.UTF8.GetString(state.Buffer, 0, bytesRead));
						// Check for end-of-file tag. If it is not there, read more data.
						string content = state.Bldr.ToString();
						if (content.Contains("<EOF>"))
						{
							string[] msg = content.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
							if (VerbosityLevel >= 2)
								Console.WriteLine("IPCHost[{0}].HostReceiveCallback() - message = \"{1}\"", _endId, msg[0]);
							var methodInfo = _serviceClass.GetMethod(msg[0]);
							var parInfo = methodInfo.GetParameters();
							object[] parameters = new object[parInfo.Length];
							for (int i = 0; i < parInfo.Length; ++i)
							{
								var typeName = parInfo[i].ParameterType.FullName;
								if (typeName == "System.String")
									parameters[i] = msg[i+1];
								else if (typeName == "System.Boolean")
									parameters[i] = msg[i+1] == true.ToString();
								else if (typeName == "System.Int32")
									parameters[i] = Int32.Parse(msg[i+1]);
							}
							methodInfo.Invoke(state.Service, parameters);
							if (VerbosityLevel >= 1)
								Console.WriteLine("IPCHost[{0}].HostReceiveCallback(): finished executing {1}", _endId, msg[0]);
							object[] attributes = methodInfo.GetCustomAttributes(typeof(FinishServerTask), true);
							if (attributes.Length > 0)
								done = true;
							state.Bldr.Clear();
							if (VerbosityLevel >= 2)
								Console.WriteLine("IPCHost[{0}].HostReceiveCallback(): calling handler.Send(\"finish:{1}\") - done = {2}", _endId, msg[0], done);
							handler.Send(Encoding.UTF8.GetBytes("finish:"+msg[0]+"\n<EOF>"));
						}
						else
						{
							// Not all data received. Get more.
						}
						if (!done)
						{
							if (VerbosityLevel >= 2)
								Console.WriteLine("IPCHost[{0}].HostReceiveCallback(): calling handler.BeginReceive(..., HostReceiveCallback, ...)", _endId);
							handler.BeginReceive(state.Buffer, 0, UnixIPCState.BufferSize, 0,
							                     new AsyncCallback(HostReceiveCallback), state);
						}
					}
				}
				catch (Exception e)
				{
					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCHost[{0}].HostReceiveCallback() - caught exception: {1}", _endId, e.Message);
					if (_alert != null)
						_alert();
					if (_cleanup != null)
						_cleanup();
				}
			}

			private class UnixIPCState
			{
				// Client  socket.
				public Socket WorkSocket { get; set; }
				// Size of receive buffer.
				public const int BufferSize = 1024;
				// Receive buffer.
				public byte[] Buffer { get; private set; }
				// Received data string, filled from buffer during one or more BeginReceive callbacks.
				public StringBuilder Bldr { get; private set; }
				// service object (instance of serviceClass)
				public object Service { get; private set; }

				public UnixIPCState(Type serviceClass)
				{
					Buffer = new byte[BufferSize];
					Bldr = new StringBuilder();
					var con = serviceClass.GetConstructor(new Type[0]);
					Service = con.Invoke(new object[0]);
				}
			}
		}
#else
		private class WindowsIPCHost : IIPCHost
		{
			ServiceHost _host;

			#region Implement IIPCHost methods
			public bool Initialize<TClass, TInterface>(string connectionId, SimpleCallback alert, SimpleCallback cleanup)
			{
				try
				{
					var hostPipeBinding = new NetNamedPipeBinding { ReceiveTimeout = TimeSpan.MaxValue };
					//open host ready for business
					_host = new ServiceHost(typeof(TClass), new[] { new Uri("net.pipe://localhost/" + connectionId) });
					_host.AddServiceEndpoint(typeof(TInterface), hostPipeBinding, "FLExPipe");
					_host.Open();
				}
				catch (InvalidOperationException)
					// Can happen if Conflict Report is open and we try to run FLExBridge again.
				{
					if (_host != null)
						((IDisposable)_host).Dispose();
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
				catch
				{
				}
				_host = null;
			}

			public int VerbosityLevel { get; set; }
			#endregion
		}
#endif		
	}
}
