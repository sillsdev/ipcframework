using System;

namespace IPCFramework
{
	/// <summary>
	/// Simplest possible callback: no parameters, no return value.
	/// </summary>
	public delegate void SimpleCallback();

	/// <summary>
	/// Host class for interprocess communication.  Somewhat analagous to ServiceHost in WCF.
	/// </summary>
	public interface IIPCHost
	{
		/// <Summary>
		/// Initialize a host connection.  Return false if it fails to connect.
		/// </Summary>
		/// <param name="connectionId">
		/// identification string for the connection.  This should look like a relative pathname with forward slashes ("/").
		/// </param>
		/// <param name="alert">
		/// callback method to notify the host to wake up and resume if it is waiting
		/// </param>
		/// <param name="cleanup">
		/// callback method for the host to do any cleanup it needs after using the connection
		/// </param>
		bool Initialize<TClass,TInterface>(string connectionId, SimpleCallback alert, SimpleCallback cleanup);

		/// <Summary>
		/// Close a host connection.
		/// </Summary>
		void Close();
		
		/// <summary>
		/// Flag whether to write verbose debugging output to the console.
		/// </summary>
		int VerbosityLevel { get; set; }
	}

	/// <summary>
	/// Client class for interprocess communication.  Somewhat analagous to channels created by ChannelFactory in WCF.
	/// </summary>
	public interface IIPCClient
	{
		/// <Summary>
		/// Initialize a client connection.  Return false if it fails to connect.
		/// </Summary>
		/// <param name="connectionId">
		/// identification string for the connection.  This should look like a relative pathname with forward slashes ("/").
		/// </param>
		/// <param name="waitObject">
		/// object shared with the client to synchronize across threads with Monitor
		/// </param>
		/// <param name="cleanup">
		/// callback method for the client to do any cleanup it needs after using the connection.  This can be null.
		/// </param>
		/// <returns>
		/// true if successful, false if an error occurs
		/// </returns>
		bool Initialize<TInterface>(string connectionId, object waitObject, SimpleCallback cleanup);

		/// <summary>
		/// Call a synchronous remote method over the connection.
		/// </summary>
		/// <param name="rpcMethod">
		/// name of a remote method to call over the connection.
		/// </param>
		/// <returns>
		/// true if successful, false if an error occurs
		/// </returns>
		bool RemoteCall(string rpcMethod);

		/// <summary>
		/// Call a synchronous remote method over the connection.
		/// </summary>
		/// <param name="rpcMethod">
		/// name of a remote method to call over the connection.
		/// </param>
		/// <param name="args">
		/// array of parameters for the method.  Only string, bool, and int (Int32) type parameters are supported.
		/// </param>
		/// <returns>
		/// true if successful, false if an error occurs
		/// </returns>
		bool RemoteCall(string rpcMethod, object[] args);

		/// <summary>
		/// Call an asynchronous remote method over the connection.
		/// </summary>
		/// <param name="rpcMethod">
		/// name of an asynchronous remote method to call over the connection.
		/// </param>
		/// <param name="signalDone">
		/// callback method to tell the client that the asynchronous call has completed.
		/// </param>
		/// <returns>
		/// true if successful, false if an error occurs
		/// </returns>
		bool RemoteCall(string rpcMethod, SimpleCallback signalDone);

		/// <summary>
		/// Call an asynchronous remote method over the connection.
		/// </summary>
		/// <param name="rpcMethod">
		/// name of an asynchronous remote method to call over the connection.
		/// </param>
		/// <param name="args">
		/// array of parameters for the method.  Only string, bool, and int (Int32) type parameters are supported.
		/// </param>
		/// <param name="signalDone">
		/// callback method to tell the client that the asynchronous call has completed.
		/// </param>
		/// <returns>
		/// true if successful, false if an error occurs
		/// </returns>
		bool RemoteCall(string rpcMethod, object[] args, SimpleCallback signalDone);

		/// <summary>
		/// Close the client connection.
		/// </summary>
		void Close();
		
		/// <summary>
		/// Flag whether to write verbose debugging output to the console.
		/// </summary>
		int VerbosityLevel { get; set; }
	}
}
