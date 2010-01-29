using System;
using Buffers.Managers;
using Buffers.Memory;

namespace Buffers.Program
{
	class GroupAccessor
	{
		private RandomDataGenerator generator = new RandomDataGenerator();
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
				generator.Generate(data);
				group.Write(query.PageId, data);
			}
		}
	}

	class RandomDataGenerator
	{
		byte cur = 1;

		public void Generate(byte[] data)
		{
			for (int i = 0; i < data.Length; i++)
				data[i] = cur++;
		}
	}
}
