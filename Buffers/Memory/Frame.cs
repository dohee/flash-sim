using System;

namespace Buffers.Memory
{
	public class Frame : IFrame
	{
        public uint Id { get; set; }
        public bool Dirty { get; set; }
        public int DataSlotId { get; set; }
        public bool Resident { get { return DataSlotId >= 0; } }

		public Frame(uint id)
			: this(id, -1) { }

		public Frame(uint id, int slotid)
		{
			this.Id = id;
            this.Dirty = false;
            this.DataSlotId = slotid;
		}

		public override string ToString()
		{
			return string.Format("Frame{{Id={0},Dirty={1},SlotId={2}}}",
                Id, Dirty, DataSlotId);
		}
	}
}
