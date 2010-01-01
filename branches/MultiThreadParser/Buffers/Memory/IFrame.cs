using System;

namespace Buffers.Memory
{
	public interface IFrame
	{
		uint Id { get; set; }
		bool Dirty { get; set; }
		bool Resident { get; }
		int DataSlotId { get; set; }
	}
}
