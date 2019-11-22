using System;

namespace IPCFramework
{
	/// <summary>
	/// Tags a method in a class as one that finishes the current task that
	/// uses interprocess communication.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class FinishServerTaskAttribute : Attribute
	{
		public override string ToString()
		{
			return "Finished!";
		}
	}
}