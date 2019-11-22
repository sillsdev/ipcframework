using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Mono.Unix;

namespace IPCFramework
{
	// ReSharper disable once InconsistentNaming
	internal class UnixIPCClient : IIPCClient
	{
		private object         _waitObject;
		private SimpleCallback _cleanup;
		private Socket         _sock;
		private SimpleCallback _signalDone;
		private string         _endId;
		private string         _rpcMethod;

		/// <summary>
		/// This code is currently specific to the FLExBridge/FLEx usage of this library.
		/// It will return a pipe identifier which will fit under the UNIX limit for a
		/// named UNIX socket.
		/// </summary>
		/// <returns>The end identifier.</returns>
		/// <param name="connectionId">Connection identifier.</param>
		private static string ComputeEndId(string connectionId)
		{
			var sb = new StringBuilder();
			sb.Append(connectionId.StartsWith("FLExBridgeEndpoint") ? "Bridge-" : "FLEx-");
			var idxBegin = connectionId.IndexOf(".fwdata", StringComparison.Ordinal);
			int idxEnd;
			if (idxBegin > 0)
			{
				idxBegin += 7;
				idxEnd = connectionId.IndexOf("_", idxBegin, StringComparison.Ordinal);
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
				var id = UnixIPCHost.TruncateId(connectionId);
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
			// ReSharper disable once EmptyGeneralCatchClause
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
		private static byte[] CreateSendData (string rpcMethod, IReadOnlyCollection<object> args)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(rpcMethod);
			if (args != null && args.Count > 0)
			{
				foreach (var arg in args)
					stringBuilder.AppendFormat("\n{0}", arg);
			}
			stringBuilder.Append("\n<EOF>");
			return Encoding.UTF8.GetBytes(stringBuilder.ToString());
		}

		private class ReturnValueHelp
		{
			public string MethodName { get; }
			public byte[] Buffer { get; }
			public Socket Socket { get; }
			public SimpleCallback SignalDone { get; }
			public string EndId { get; }

			public ReturnValueHelp(string method, Socket socket, SimpleCallback signalDone, string endId)
			{
				MethodName = method;
				Buffer = new byte[1024];
				Socket = socket;
				SignalDone = signalDone;
				EndId = endId;
			}
		}

		/// <summary>
		/// Wait to receive acknowledgement from the remote end.
		/// </summary>
		/// <returns>string value of the return packet</returns>
		private void GetReturnValue()
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
				var retVal = Encoding.UTF8.GetString(help.Buffer, 0, len);
				if (VerbosityLevel >= 1)
					Console.WriteLine("IPCClient[{0}].ReceiveCallback() for {1} - _sock.EndReceive() => \"{2}\"",
						help.EndId, help.MethodName, retVal);
				help.SignalDone?.Invoke();
				_cleanup?.Invoke();
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
				_signalDone?.Invoke();
				Monitor.Pulse(_waitObject);
				if (VerbosityLevel >= 1)
					Console.WriteLine("IPCClient[{0}].SendCallback() for {1} - caught SocketException and woke up Host (?)",
						_endId, _rpcMethod);
				_cleanup?.Invoke();
			}
			catch (Exception e)
			{
				if (VerbosityLevel >= 1)
					Console.WriteLine("IPCClient[{0}].SendCallback() for {1} - caught Exception: {2}",
						_endId, _rpcMethod, e.Message);
				_cleanup?.Invoke();
			}
			finally
			{
				// Do NOT call cleanup here, we don't want to cleanup until we either get the result or fail.
				Monitor.Exit(_waitObject);
			}
		}
	}
}