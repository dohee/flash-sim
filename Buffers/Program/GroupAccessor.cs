using System;
using Buffers.Managers;
using Buffers.Memory;

namespace Buffers.Program
{
	class GroupAccessor
	{
		private readonly ManagerGroup group;
		private readonly byte[] data;
		private readonly Random rand;

		public GroupAccessor(ManagerGroup group, bool generateData)
		{
			this.group = group;
			this.data = new byte[group.PageSize];
			this.rand = (generateData ? new Random() : null);
		}

		public void Access(RWQuery query)
		{
			if (query.Type == AccessType.Read)
			{
				group.Read(query.PageId, data);
			}
			else
			{
				if (rand != null)
					rand.NextBytes(data);
				group.Write(query.PageId, data);
			}
		}
	}

}
