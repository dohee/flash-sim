namespace Buffers.FTL
{
	public sealed class BlockState
	{
		public ushort FreePageCount { get; set; }
		public ushort AllcPageCount { get; set; }
		public ushort LivePageCount { get; set; }
		public ushort DeadPageCount { get; set; }
		public PageState[] PageStates { get; private set; }

		public BlockState(ushort pageCount)
		{
			PageStates = new PageState[pageCount];
			FreePageCount = pageCount;
			AllcPageCount = 0;
			LivePageCount = 0;
			DeadPageCount = 0;
		}
	}
}