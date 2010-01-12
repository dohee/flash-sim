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
}
