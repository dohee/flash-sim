using System;
using System.Runtime.Serialization;

namespace Buffers.FTL
{
	[Serializable]
	public class FTLException : ApplicationException
	{
		public FTLException() { }
		public FTLException(string message) : base(message) { }
		public FTLException(string message, Exception inner) : base(message, inner) { }
		protected FTLException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[global::System.Serializable]
	public class FlashBlockBrokenException : FTLException
	{
		public FlashBlockBrokenException() { }
		public FlashBlockBrokenException(string message) : base(message) { }
		public FlashBlockBrokenException(string message, Exception inner) : base(message, inner) { }
		protected FlashBlockBrokenException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[global::System.Serializable]
	public class FlashNoMemoryException : FTLException
	{
		public FlashNoMemoryException() { }
		public FlashNoMemoryException(string message) : base(message) { }
		public FlashNoMemoryException(string message, Exception inner) : base(message, inner) { }
		protected FlashNoMemoryException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[global::System.Serializable]
	public class FlashNotDirtyException : FTLException
	{
		public FlashNotDirtyException() { }
		public FlashNotDirtyException(string message) : base(message) { }
		public FlashNotDirtyException(string message, Exception inner) : base(message, inner) { }
		protected FlashNotDirtyException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[global::System.Serializable]
	public class InvalidLBAException : FTLException
	{
		public InvalidLBAException() { }
		public InvalidLBAException(string message) : base(message) { }
		public InvalidLBAException(string message, Exception inner) : base(message, inner) { }
		protected InvalidLBAException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[global::System.Serializable]
	public class InvalidPageStateException : FTLException
	{
		public InvalidPageStateException() { }
		public InvalidPageStateException(string message) : base(message) { }
		public InvalidPageStateException(string message, Exception inner) : base(message, inner) { }
		protected InvalidPageStateException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}
}