using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers
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
