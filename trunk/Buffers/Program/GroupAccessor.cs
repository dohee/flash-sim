using System;
using Buffers.Managers;
using Buffers.Memory;

namespace Buffers.Program
{
	class GroupAccessor
	{
		private Random rand = new Random();
		private ManagerGroup group;
		private byte[] data;

		public GroupAccessor(ManagerGroup group)
		{
			this.group = group;
			this.data = new byte[group.PageSize];
		}

		public void Access(RWQuery query)
		{
			if (query.Type == AccessType.Read)
			{
				group.Read(query.PageId, data);
			}
			else
			{
				rand.NextBytes(data);
				group.Write(query.PageId, data);
			}
		}
	}

}
