using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ParseStrace
{
	interface IIOItemStorage : IEnumerable<IOItem>
	{
		void Add(IOItem item);		
	}

	abstract class IOItemStorageBase : IIOItemStorage
	{
		public abstract void Add(IOItem item);
		public abstract IEnumerator<IOItem> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}


	class IOItemToTextWriter : IOItemStorageBase
	{
		private TextWriter writer;

		public IOItemToTextWriter(TextWriter writer)
		{
			this.writer = writer;
		}

		public override void Add(IOItem item)
		{
			writer.WriteLine("{0}\t{1}\t{2}\t{3}",
				item.Position, item.Length,
				item.IsWrite ? 1 : 0, item.Filename);
		}

		public override IEnumerator<IOItem> GetEnumerator()
		{
			throw new NotSupportedException();
		}
	}


	
}
