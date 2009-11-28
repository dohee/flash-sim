using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ParseStrace
{
	abstract class IOItemStorage
	{
		public abstract void Add(IOItem item);
		public abstract void Output();

		protected readonly TextWriter writer;

		public IOItemStorage(TextWriter writer)
		{
			this.writer = writer;
		}
	}


	sealed class IOItemDirectlyToWriter : IOItemStorage
	{
		public IOItemDirectlyToWriter(TextWriter writer)
			: base(writer) { }

		public override void Add(IOItem item)
		{
			writer.WriteLine("{0}\t{1}\t{2}\t{3}",
				item.Position, item.Length,
				item.IsWrite ? 1 : 0, item.Filename);
		}

		public override void Output() { }
	}

	
	class IOItemWithFilenameCut : IOItemStorage
	{
		private const int kPageSize = 4096;
		protected readonly IList<IOItem> list = new List<IOItem>();
		private readonly IDictionary<string, int> filePages = new Dictionary<string, int>();

		public IOItemWithFilenameCut(TextWriter writer)
			: base(writer) { }

		public override void Add(IOItem item)
		{
			long pos = item.Position / kPageSize;
			long len = (item.Position + item.Length + (kPageSize - 1)) / kPageSize - pos;
			if (len == 0)
				len = 1;

			list.Add(new IOItem(item.Filename, item.IsWrite, pos, len));

			int maxpage = 0, npages = (int)(pos + len);
			filePages.TryGetValue(item.Filename, out maxpage);

			if (npages > maxpage)
				filePages[item.Filename] = npages;
		}

		public override void Output()
		{
			var fileStarts = CalcFileStart();

			foreach (IOItem item in list)
			{
				writer.WriteLine("{0}\t{1}\t{2}",
					item.Position + fileStarts[item.Filename],
					item.Length, item.IsWrite ? 1 : 0);
			}
		}

		protected IDictionary<string, int> CalcFileStart()
		{
			var fileStarts = new Dictionary<string, int>();
			int start = 0;

			foreach (var item in filePages)
			{
				fileStarts[item.Key] = start;
				start += item.Value;
			}

			return fileStarts;
		}
	}


	sealed class IOItemStorageVerbose : IOItemWithFilenameCut
	{
		private readonly IList<IOItem> origin = new List<IOItem>();

		public IOItemStorageVerbose(TextWriter writer)
			: base(writer) { }

		public override void Add(IOItem item)
		{
			origin.Add(item);
			base.Add(item);
		}

		public override void Output()
		{
			var fileStarts = CalcFileStart();
			IOItemDirectlyToWriter directly = new IOItemDirectlyToWriter(writer);

			for (int i = 0; i < origin.Count; i++)
			{
				writer.Write("# ");
				directly.Add(origin[i]);

				IOItem item = list[i];
				writer.WriteLine("{0}\t{1}\t{2}",
					item.Position + fileStarts[item.Filename],
					item.Length, item.IsWrite ? 1 : 0);
			}
		}
	}
	
}
