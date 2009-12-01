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

		public override void Output() { }

		public override void Add(IOItem item)
		{
			Output(writer, item);
		}

		public static void Output(TextWriter writer, IOItem item)
		{
			writer.WriteLine("{0}\t{1}\t{2}  FD={3}{4}: {5}",
				item.Position,
				item.Length,
				item.IsWrite ? 'W' : 'R',
				item.FD,
				item.IsTerminal ? "<Term>" : "",
				item.Filename);
		}
	}

	
	sealed class IOItemStorageVerbose : IOItemStorage
	{
		private const int kPageSize = 4096;
		private readonly IList<IOItem> origin = new List<IOItem>();

		public IOItemStorageVerbose(TextWriter writer)
			: base(writer) { }

		public override void Add(IOItem item)
		{
			origin.Add(item);
		}

		public void CalcPagePosition(IOItem item, out long pos, out long len)
		{
			pos = item.Position / kPageSize;
			len = (item.Position + item.Length + (kPageSize - 1)) / kPageSize - pos;

			if (len == 0)
				len = 1;
		}

		public override void Output()
		{
			var fileStarts = CalcFileStart();

			foreach (var item in origin)
			{
				if (!item.IsTerminal)
				{
					long pos, len;
					CalcPagePosition(item, out pos, out len);

					writer.Write("{0}\t{1}\t{2}     #\t",
						pos + fileStarts[item.Filename],
						len, item.IsWrite ? 1 : 0);
				}
				else
				{
					writer.Write("\t\t      #\t");
				}

				IOItemDirectlyToWriter.Output(writer, item);
			}
		}

		private IDictionary<string, int> CalcFileStart()
		{
			var filePages = new Dictionary<string, int>();
			var fileStarts = new Dictionary<string, int>();
			int start = 0;

			foreach (var item in origin)
			{
				long pos, len;
				CalcPagePosition(item, out pos, out len);

				int maxpage = 0, npages = (int)(pos + len);
				filePages.TryGetValue(item.Filename, out maxpage);

				if (npages > maxpage)
					filePages[item.Filename] = npages;
			}

			foreach (var item in filePages)
			{
				fileStarts[item.Key] = start;
				start += item.Value;
			}

			return fileStarts;
		}
	}
	
}
