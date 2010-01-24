using System;


namespace Buffers
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	sealed class ManagerFactoryAttribute : Attribute
	{
		public ManagerFactoryAttribute(string CmdLineName)
		{
			this.CmdLineName = CmdLineName;
		}

		public string CmdLineName { get; private set; }
	}
}
