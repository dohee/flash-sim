using System;


namespace Buffers.Memory
{
    //for Lirs-byWu.cs
    class lirFrame : Frame
    {

        public bool IsHir { get; set; }

        public lirFrame(uint id) : base(id) { }
        public lirFrame(uint id, int slotid) : base(id, slotid) { }
        public override string ToString()
        {
            return string.Format("Frame{{Id={0},Dirty={1},SlotId={2},IsHir={3}}}",
                Id, Dirty, DataSlotId, IsHir);
        }
    }   
}