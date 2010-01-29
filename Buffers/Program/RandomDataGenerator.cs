using System;

namespace Buffers.Program
{
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
