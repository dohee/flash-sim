using System;
using System.Runtime.Serialization;

namespace Buffers
{

	[Serializable]
	public class DataNotConsistentException : ApplicationException
	{
		public DataNotConsistentException() { }
		public DataNotConsistentException(string message) : base(message) { }
		public DataNotConsistentException(string message, Exception inner)
			: base(message, inner) { }
		protected DataNotConsistentException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class InvalidCmdLineArgumentException : ApplicationException
	{
		public InvalidCmdLineArgumentException() { }
		public InvalidCmdLineArgumentException(string message) : base(message) { }
		public InvalidCmdLineArgumentException(string message, Exception inner)
			: base(message, inner) { }
		protected InvalidCmdLineArgumentException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }
	}
}
