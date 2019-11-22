using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Mono.Unix;

// ReSharper disable InconsistentNaming

namespace IPCFramework
{
	internal class UnixIPCHost : IIPCHost
	{
		private Socket         _host;
		private Type           _serviceClass;
		private SimpleCallback _alert;
		private SimpleCallback _cleanup;

		private string _endId;

		~UnixIPCHost()
		{
			Close();
		}

		#region Implement IIPCHost methods
		public bool Initialize<TClass,TInterface>(string connectionId, SimpleCallback alert, SimpleCallback cleanup)
		{
			var id = TruncateId(connectionId);
			_endId = ComputeEndId(connectionId);
			_host = null;
			_serviceClass = typeof(TClass);
			_alert = alert;
			_cleanup = cleanup;
			try
			{
				_host = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
				if (VerbosityLevel >= 1)
					Console.WriteLine("IPCHost[{0}].Initialize(): binding to {1}", _endId, id);
				var endPoint = new AbstractUnixEndPoint(id);
				_host.Bind(endPoint);
				_host.Listen(5);
				_host.BeginAccept(SocketAcceptCallback, _host);
			}
			catch (Exception ex)
			{
				if (VerbosityLevel >= 1)
					Console.WriteLine("IPCHost[{0}].Initialize - caught exception: {1}", _endId, ex.Message);
				_host?.Close();
				_host = null;
				return false;
			}
			return true;
		}

		public void Close()
		{
			try
			{
				_host?.Close();
				_host = null;
			}
			catch (Exception)
			{
				_host = null;
			}
		}

		public int VerbosityLevel { get; set; }
		#endregion

		/// <summary>
		/// Truncate the specified connectionId.  The byte array for a socket address
		/// derived from this string has a maximum length of 107 (+1 for the leading
		/// NUL byte that indicates an "abstract" connection).
		/// </summary>
		internal static string TruncateId(string id)
		{
			var min = id.IndexOf('/');
			var lim = id.LastIndexOf('/');
			if (min < lim)
			{
				var x = id.Remove(min, lim - min);
				x = x.Replace(".fwdata","/");
				return x;
			}
			return id.Replace(".fwdata", "/");
		}

		private static string ComputeEndId(string id)
		{
			var sb = new StringBuilder();
			sb.Append(id.StartsWith("FLExBridgeEndpoint") ? "Bridge-" : "FLEx-");
			var idxBegin = id.IndexOf(".fwdata", StringComparison.Ordinal);
			if (idxBegin >= 0)
			{
				idxBegin += 7;
				var idxEnd = id.IndexOf("_", idxBegin, StringComparison.Ordinal);
				sb.Append(idxEnd > idxBegin
					? id.Substring(idxBegin, idxEnd - idxBegin)
					: id.Substring(idxBegin));
			}
			else
			{
				idxBegin = id.LastIndexOf("/", StringComparison.Ordinal);
				if (idxBegin > 0)
					sb.Append(id.Substring(idxBegin));
				else
					return id;
			}
			return sb.ToString();
		}

		/// <summary>
		/// This callback operates on its own thread.  After finishing the Accept operation,
		/// start an asynchronous BeginReceive operation on yet another thread.  After
		/// that, start checking for errors on the socket once a second (for a blocking host).
		/// </summary>
		/// <param name="ar">Ar.</param>
		private void SocketAcceptCallback(IAsyncResult ar)
		{
			// Get the socket that handles the client request.
			var host = (Socket)ar.AsyncState;
			var handler = host.EndAccept(ar);
			// Create the state object.
			var state = new UnixIPCState(_serviceClass) { WorkSocket = handler };
			if (VerbosityLevel >= 2)
				Console.WriteLine("IPCHost[{0}].SocketAcceptCallback() - calling handler.BeginReceive(..., HostReceiveCallback, ...)", _endId);
			handler.BeginReceive(state.Buffer, 0, UnixIPCState.BufferSize, 0,
				HostReceiveCallback, state);
			while (_host != null)
			{
				if (handler.Poll(10000, SelectMode.SelectError))
				{
					if (VerbosityLevel >= 2)
						Console.WriteLine("IPCHost[{0}].SocketAcceptCallback() - calling alert and cleanup delegates", _endId);
					_alert?.Invoke();
					_cleanup?.Invoke();
					break;
				}
				Thread.Sleep(1000);
			}
		}

		private void HostReceiveCallback(IAsyncResult ar)
		{
			if (VerbosityLevel >= 2)
				Console.WriteLine("IPCHost[{0}].HostReceiveCallback()", _endId);
			var done = false;
			var state = (UnixIPCState)ar.AsyncState;
			var handler = state.WorkSocket;
			try
			{
				// Read data from the client socket.
				var bytesRead = handler.EndReceive(ar);
				if (VerbosityLevel >= 2)
					Console.WriteLine("IPCHost[{0}].HostReceiveCallback() - handler.EndReceive() read {1} bytes", _endId, bytesRead);
				if (bytesRead <= 0 && state.Bldr.Length <= 0)
					return;

				// There might be more data to come. Store the data received so far.
				state.Bldr.Append(Encoding.UTF8.GetString(state.Buffer, 0, bytesRead));
				// Check for end-of-file tag. If it is not there, read more data.
				var content = state.Bldr.ToString();

				// ReSharper disable once InconsistentNaming
				const string EOF="<EOF>";
				if (content.Contains(EOF))
				{
					// ReSharper disable once CommentTypo

					// The buffer could contain data from requests for both InformFwProjectName and
					// BridgeWorkComplete, rather than just one, as seen in LT-19122. If so, cut off
					// the first set of data, ending with "<EOF>", and save the rest of the data in
					// state.Bldr so it can be used the next time HostReceiveCallback is run. This would
					// probably do well to be a loop instead.
					var remnants = content.Substring(content.IndexOf(EOF, StringComparison.InvariantCulture) + EOF.Length);
					content = content.Substring(0, content.IndexOf(EOF, StringComparison.InvariantCulture) + EOF.Length);
					state.Bldr.Clear();
					if (!string.IsNullOrEmpty(remnants))
					{
						state.Bldr.Append(remnants);
					}

					var msg = content.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
					if (VerbosityLevel >= 2)
						Console.WriteLine("IPCHost[{0}].HostReceiveCallback() - message = \"{1}\"", _endId, msg[0]);
					var methodInfo = _serviceClass.GetMethod(msg[0]);
					if (methodInfo != null)
					{
						var parInfo = methodInfo.GetParameters();
						var parameters = new object[parInfo.Length];
						for (var i = 0; i < parInfo.Length; ++i)
						{
							var typeName = parInfo[i].ParameterType.FullName;
							if (typeName == "System.String")
								parameters[i] = msg[i+1] == "<EOF>" ? "" : msg[i+1];
							else if (typeName == "System.Boolean")
								parameters[i] = msg[i+1] == true.ToString();
							else if (typeName == "System.Int32")
								parameters[i] = int.Parse(msg[i+1]);
						}
						methodInfo.Invoke(state.Service, parameters);
					}

					if (VerbosityLevel >= 1)
						Console.WriteLine("IPCHost[{0}].HostReceiveCallback(): finished executing {1}", _endId, msg[0]);
					if (methodInfo != null)
					{
						var attributes = methodInfo.GetCustomAttributes(typeof(FinishServerTaskAttribute), true);
						if (attributes.Length > 0)
							done = true;
					}

					if (VerbosityLevel >= 2)
						Console.WriteLine("IPCHost[{0}].HostReceiveCallback(): calling handler.Send(\"finish:{1}\") - done = {2}", _endId, msg[0], done);
					handler.Send(Encoding.UTF8.GetBytes("finish:"+msg[0]+"\n<EOF>"));
				}
				else
				{
					// Not all data received. Get more.
				}

				if (done)
					return;

				if (VerbosityLevel >= 2)
					Console.WriteLine("IPCHost[{0}].HostReceiveCallback(): calling handler.BeginReceive(..., HostReceiveCallback, ...)", _endId);
				handler.BeginReceive(state.Buffer, 0, UnixIPCState.BufferSize, 0,
					HostReceiveCallback, state);
			}
			catch (Exception e)
			{
				if (VerbosityLevel >= 1)
					Console.WriteLine("IPCHost[{0}].HostReceiveCallback() - caught exception: {1}", _endId, e.Message);
				_alert?.Invoke();
				_cleanup?.Invoke();
			}
		}

		private class UnixIPCState
		{
			// Client  socket.
			public Socket WorkSocket { get; set; }
			// Size of receive buffer.
			public const int BufferSize = 1024;
			// Receive buffer.
			public byte[] Buffer { get; }
			// Received data string, filled from buffer during one or more BeginReceive callbacks.
			// ReSharper disable once IdentifierTypo
			public StringBuilder Bldr { get; }
			// service object (instance of serviceClass)
			public object Service { get; }

			public UnixIPCState(Type serviceClass)
			{
				Buffer = new byte[BufferSize];
				Bldr = new StringBuilder();
				var con = serviceClass.GetConstructor(new Type[0]);
				Service = con?.Invoke(new object[0]);
			}
		}
	}
}