using System;

namespace Buffers
{
	public interface IFrame
	{
		uint Id { get; set; }
		bool Dirty { get; set; }
		bool Resident { get; }
		int DataSlotId { get; set; }
	}
}
