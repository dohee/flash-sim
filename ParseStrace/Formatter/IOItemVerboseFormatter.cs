﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ParseStrace
{
	sealed class IOItemVerboseFormatter : IOItemFormatter
	{
		private static string vimModeline = "vim: set nowrap:";

		private const int kPageSize = 4096;
		private long nlines = 0;
		private readonly IDictionary<string, int> filePages = new Dictionary<string, int>();
		private readonly IDictionary<string, int> fileStarts = new Dictionary<string, int>();
		private readonly ICollection<int> allowPIDs;

		public IOItemVerboseFormatter(TextWriter writer)
			: this(writer, null) { }
		public IOItemVerboseFormatter(TextWriter writer, ICollection<int> pids)
			: base(writer)
		{
			if (pids == null)
			{
				allowPIDs = null;
			}
			else
			{
				allowPIDs = new HashSet<int>();
				foreach (var pid in pids)
					allowPIDs.Add(pid);
			}
		}

		public void CalcPagePosition(IOItem item, out long pos, out long len)
		{
			pos = item.Position / kPageSize;
			len = (item.Position + item.Length + (kPageSize - 1)) / kPageSize - pos;

			if (len == 0)
				len = 1;
		}

		public override void PhaseBefore(FormatterInfo info)
		{
			writer.WriteLine("# Original Strace: " + info.Filename);
			writer.WriteLine("# " + vimModeline);
			writer.WriteLine("# Parsed by: {0} @ {1}", Environment.UserName, Environment.MachineName);
			writer.WriteLine("# Parsed at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			writer.WriteLine("# Parsed with: " + Environment.OSVersion);
			writer.Write("# Allowed PIDs: ");

			if (allowPIDs == null)
			{
				writer.WriteLine("All");
			}
			else
			{
				var pidstrs = from p in allowPIDs
							  orderby p
							  select p.ToString();

				writer.WriteLine(string.Join(" ", pidstrs.ToArray()));
			}
	
			nlines += 6;
		}

		public override void PhaseOne(IOItem item)
		{
			nlines++;

			long pos, len;
			CalcPagePosition(item, out pos, out len);

			int maxpage = 0, npages = (int)(pos + len);
			filePages.TryGetValue(item.Filename, out maxpage);

			if (npages > maxpage)
				filePages[item.Filename] = npages;
		}

		public override void PhaseBetween()
		{
			nlines += 2;
			writer.WriteLine("# Lines: " + nlines);
			int start = 0;

			foreach (var item in filePages)
			{
				fileStarts[item.Key] = start;
				start += item.Value;
			}
		}

		public override void PhaseTwo(IOItem item)
		{
			if (item.FDType == FDType.File &&
				(allowPIDs == null || allowPIDs.Contains(item.Pid)))
			{
				long pos, len;
				CalcPagePosition(item, out pos, out len);

				writer.Write("{0}\t{1}\t{2}\t# ",
					pos + fileStarts[item.Filename], len,
					(item.Access & AccessType.Write) == 0 ? 0 : 1);
			}
			else
			{
				writer.Write("\t\t\t# ");
			}

			IOItemDirectlyToWriter.Output(writer, item);
		}

		public override void PhaseAfter()
		{
			writer.WriteLine("# " + vimModeline);
		}
	}

}