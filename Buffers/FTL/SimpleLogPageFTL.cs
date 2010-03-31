using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Buffers.Devices;
using Buffers.Utilities;
using LogicalId = System.UInt32;
using BlockId = System.UInt32;

namespace Buffers.FTL
{
	public sealed class SimpleLogPageFTL : FTLBase
	{
		private BlockState[] blockStates;

		private BlockPageId[] mapList;
		private LogicalId[] reverseMapList;
		private uint mapListPoint;

		private List<BlockId> freeList = new List<uint>();
		private List<BlockId> dirtyList = new List<uint>();
		private List<BlockId> deadList = new List<uint>();

		private BlockId reserved;

		private uint mapListSize;
		private int wearLevelingThreshold;
		private uint blockCount;


		public SimpleLogPageFTL(IErasableDevice device,
			uint blockCount, uint mapListSize, int wearLevelingThreshold)
			: base(device)
		{
			this.blockCount = blockCount;
			this.mapListSize = mapListSize;
			this.wearLevelingThreshold = wearLevelingThreshold;

			// initialize the lba-pba map lists
			mapList = new BlockPageId[mapListSize];
			for (int i = 0; i < mapList.Length; i++)
				mapList[i] = new BlockPageId(uint.MaxValue, ushort.MaxValue);

			reverseMapList = new uint[device.PageCountPerBlock * blockCount];
			for (int i = 0; i < reverseMapList.Length; i++)
				reverseMapList[i] = uint.MaxValue;

			mapListPoint = 0;

			// initialize free/dirty block lists
			for (uint i = 0; i < blockCount - 1; i++)
				freeList.Add(i);

			reserved = blockCount - 1;	// set the last block as the reserved block

			// initialize block state array
			blockStates = new BlockState[blockCount];
			for (int i = 0; i < blockStates.Length; i++)
				blockStates[i] = new BlockState(dev.PageCountPerBlock);
		}

		public override string Description
		{
			get
			{
				return "I dont know...";
			}
		}

		public LogicalId[] AllocPage(int count)
		{
			Debug.Assert(count > 0);
			int allocated = 0;	/* allocated lba count */
			LogicalId[] lbas = new LogicalId[count];

			for (int j = 0; j < mapListSize; j++)
			{
				BlockPageId pba = TranslateLBAtoPBA(mapListPoint);

				if (pba.IsInvalid) // empty lba
				{
					// allocate a new page
					pba = AllocNewPage();

					if (pba.IsInvalid) // no more space free
					{
						Array.Resize(ref lbas, allocated);
						return lbas;
					}

					lbas[allocated] = mapListPoint;

					// register new mapping entry
					RegisterEntry(mapListPoint, pba);

					if (++allocated == count) // allocation complete
					{
						mapListPoint = (mapListPoint + 1) % mapListSize;
						return lbas;
					}
				}

				mapListPoint = (mapListPoint + 1) % mapListSize;
			}

			Array.Resize(ref lbas, allocated);
			return lbas;
		}

		public void ReleasePage(LogicalId lba)
		{
			BlockPageId pba = TranslateLBAtoPBA(lba);

			if (pba.IsInvalid)
				throw new InvalidLBAException();

			PageState ps = GetPageState(pba);

			switch (ps)
			{
				case PageState.Free:
				case PageState.Dead:
					throw new InvalidPageStateException();
				case PageState.Allc: // reclaim unused page
					SetPageState(pba, PageState.Free);
					break;
				case PageState.Live:
					SetPageState(pba, PageState.Dead);
					break;
			}

			// reset lba-pba list
			RegisterEntry(lba, BlockPageId.Invalid);
		}

		protected override void DoRead(uint pageid, byte[] result)
		{
			Debug.Assert(result != null);
			BlockPageId pba = TranslateLBAtoPBA(pageid);

			if (pba.IsInvalid)
				throw new InvalidLBAException();

			dev.Read(dev.ToUniversalPageId(pba), result);
		}

		protected override void DoWrite(uint pageid, byte[] data)
		{
			Debug.Assert(data != null);
			BlockPageId pba = TranslateLBAtoPBA(pageid);

			if (pba.IsInvalid)
				throw new InvalidLBAException();

			PageState ps = GetPageState(pba);

			if (ps == PageState.Dead || ps == PageState.Free)
				throw new InvalidPageStateException();

			if (ps == PageState.Live)
			{
				BlockPageId pba2 = AllocNewPage();
				pba = TranslateLBAtoPBA(pageid);

				if (pba2.IsInvalid)
					throw new FlashNoMemoryException();

				SetPageState(pba, PageState.Dead);
				RegisterEntry(pageid, pba2);
				pba = pba2;
			}

			dev.Write(dev.ToUniversalPageId(pba), data);
			SetPageState(pba, PageState.Live);
		}

		private BlockPageId TranslateLBAtoPBA(LogicalId lba)
		{
			Debug.Assert(lba < mapListSize);
			BlockPageId pba = mapList[lba];

			if (!pba.IsInvalid)
				Debug.Assert(reverseMapList[dev.ToUniversalPageId(pba)] == lba);

			return pba;
		}

		private void RegisterEntry(LogicalId lba, BlockPageId pba)
		{
			Debug.Assert(lba < mapListSize);
			BlockPageId p = mapList[lba];

			if (!p.IsInvalid)
				reverseMapList[dev.ToUniversalPageId(p)] = uint.MaxValue;

			mapList[lba] = pba;

			if (!pba.IsInvalid)
				reverseMapList[dev.ToUniversalPageId(pba)] = lba;
		}

		/* Allocate a new page */
		private BlockPageId AllocNewPage()
		{
			/* The strategy of space allocation here is:
			 * If there is free page in current block(the last block in dirtyList), then allocate the page,
			 * otherwise, allocate a new page in freeList. If there is no free block in freeList, then,
			 * a garbage collection will be activated.
			 */

			BlockPageId pba;

			if (dirtyList.Count == 0) // dirtyList is empty, scan freeList
			{
				if (freeList.Count == 0) // freeList is empty, scan deadList
				{
					if (deadList.Count == 0) // deadList is empty, R U KIDDING ME?!!
						return BlockPageId.Invalid;

					if (ReclaimBlock()) // re-allocate again
						return AllocNewPage();
					else   // garbage collection failed
						return BlockPageId.Invalid;
				}
				else //freeList is not empty...
				{
					// allocate the first free block
					pba = new BlockPageId(freeList[0], 0);
					SetPageState(pba, PageState.Allc);

					// move the block to dirty block list
					freeList.RemoveAt(0);
					dirtyList.Add(pba.BlockId);

					return pba;
				} //~ if (freeList.size() == 0)
			}
			else // dirtyList isn't empty, scan it
			{
				BlockId pos;

				for (pos = 0; pos < (uint)dirtyList.Count; pos++)
					if (blockStates[dirtyList[(int)pos]].FreePageCount > 0)
						break;

				if (pos >= (uint)dirtyList.Count) // No page free in dirtyList (allc, live, dead), scan freeList
				{
					if (freeList.Count == 0) // no free page in freeList, active garbage collection
					{
						if (ReclaimBlock()) // re-allocate again
							return AllocNewPage();
						else  // garbage collection failed
							return BlockPageId.Invalid;
					}
					else
					{
						pba = new BlockPageId(freeList[0], 0);
						SetPageState(pba, PageState.Allc);

						// move the block to dirty block list
						freeList.RemoveAt(0);
						dirtyList.Add(pba.BlockId);

						return pba;
					}
				}
				else // Select the unallocated page
				{
					BlockId blkid = dirtyList[(int)pos];

					for (ushort i = 0; i < dev.PageCountPerBlock; i++)
					{
						pba = new BlockPageId(blkid, i);

						if (GetPageState(pba) == PageState.Free)
						{
							SetPageState(pba, PageState.Allc);
							return pba;
						}
					}

					// no free page? It's impossible!
					return BlockPageId.Invalid;	// impossible return~~~~
				} // ~if (pos >= dirtyList.Count)
			} // ~if (dirtyList.Count == 0)
		}

		/* Reclaim a block */
		private bool ReclaimBlock()
		{
			BlockId mostDirtyBlockID = BlockId.MaxValue, leastEraseBlockID = BlockId.MaxValue;
			int mostDirtyCount = -1, leastEraseCount = int.MaxValue;
			int mostDirtyIndex = -1, leaseEraseIndex = -1;

			ushort pageCountPerBlock = dev.PageCountPerBlock;
			byte[] buffer = null;

			if (deadList.Count > 0)
			{
				// Erase blocks in deadList
				foreach (BlockId blkid in deadList)
				{
					// erase all blocks
					dev.Erase(blkid);
					for (ushort i = 0; i < pageCountPerBlock; i++)
						SetPageState(new BlockPageId(blkid, i), PageState.Free);

					freeList.Add(blkid);
				}

				// clear deadList
				deadList.Clear();

				return true;
			}

			if (dirtyList.Count == 0) // dirtyList is empty, no page can be reclaimed.
				return false; // throw new FlashNotDirtyException();


			// Get the most dirty block and the least erase block from dirtyList
			for (int i = 0; i < dirtyList.Count; i++)
			{
				BlockId blkid = dirtyList[i];

				if (blockStates[blkid].DeadPageCount > mostDirtyCount)
				{
					mostDirtyCount = blockStates[blkid].DeadPageCount;
					mostDirtyIndex = i;
					mostDirtyBlockID = blkid;
				}
				if (dev.GetBlockEraseCount(blkid) < leastEraseCount)
				{
					leastEraseCount = dev.GetBlockEraseCount(blkid);
					leastEraseCount = i;
					leastEraseBlockID = blkid;
				}
			}

			if (mostDirtyCount <= 0) // no block can be reclaimed
				return false; // throw new FlashNotDirtyException();

			buffer = new byte[PageSize];

			// Wear leveling
			if (dev.GetBlockEraseCount(mostDirtyBlockID) - leastEraseCount > wearLevelingThreshold)
			{
				// Just do it
				ushort index = 0;

				// Move live/allocated pages of leastEraseBlock to reserved block
				for (ushort i = 0; i < pageCountPerBlock; i++)
				{
					if (GetPageState(new BlockPageId(leastEraseBlockID, i)) == PageState.Live)
					{
						BlockPageId pba = new BlockPageId(reserved, index);
						dev.Read(dev.ToUniversalPageId(leastEraseBlockID, i), buffer);
						dev.Write(dev.ToUniversalPageId(pba), buffer);
						SetPageState(pba, PageState.Live);

						// Update LBA-PBA list
						LogicalId lba = reverseMapList[dev.ToUniversalPageId(leastEraseBlockID, i)];
						RegisterEntry(lba, pba);

						++index;
					}
					else if (GetPageState(new BlockPageId(leastEraseBlockID, i)) == PageState.Allc)
					{
						BlockPageId pba = new BlockPageId(reserved, index);
						SetPageState(pba, PageState.Allc);

						// Update LBA-PBA list
						LogicalId lba = reverseMapList[dev.ToUniversalPageId(leastEraseBlockID, i)];
						RegisterEntry(lba, pba);

						++index;
					}
				}

				// Erase leastEraseBlock block
				dev.Erase(leastEraseBlockID);

				for (ushort i = 0; i < pageCountPerBlock; i++)
					SetPageState(new BlockPageId(leastEraseBlockID, i), PageState.Free);

				index = 0;

				// Move live pages of mostDirtyBlockID block to leaseEraseBlockID block
				for (ushort i = 0; i < pageCountPerBlock; i++)
				{
					if (GetPageState(new BlockPageId(mostDirtyBlockID, i)) == PageState.Live)
					{
						// move to reserved block
						BlockPageId pba = new BlockPageId(leastEraseBlockID, index);
						dev.Read(dev.ToUniversalPageId(mostDirtyBlockID, i), buffer);
						dev.Write(dev.ToUniversalPageId(pba), buffer);
						SetPageState(pba, PageState.Live);

						// Update LBA-PBA list
						LogicalId lba = reverseMapList[dev.ToUniversalPageId(mostDirtyBlockID, i)];
						RegisterEntry(lba, pba);

						index++;
					}
					else if (GetPageState(new BlockPageId(mostDirtyBlockID, i)) == PageState.Allc)
					{
						BlockPageId pba = new BlockPageId(leastEraseBlockID, index);
						SetPageState(pba, PageState.Allc);

						// Update LBA-PBA list
						LogicalId lba = reverseMapList[dev.ToUniversalPageId(mostDirtyBlockID, i)];
						RegisterEntry(lba, pba);

						index++;
					}
				}

				// Erase mostDirtyBlockID block
				dev.Erase(mostDirtyBlockID);

				for (ushort i = 0; i < pageCountPerBlock; i++)				
					SetPageState(new BlockPageId(mostDirtyBlockID, i), PageState.Free);

				// Remove mostDirtyBlockID block and leastEraseBlockID block from dirtyList
				dirtyList.RemoveAt(mostDirtyIndex);

				// Add reserved block and leastEraseBlockID block into dirtyList
				dirtyList.Add(reserved);

				reserved = mostDirtyBlockID;
			}
			else
			{
				ushort index = 0;

				// Move live/allocated pages of mostDirtyBlockID block to reserved block
				for (ushort i = 0; i < pageCountPerBlock; i++)
				{
					BlockPageId bpid = new BlockPageId(mostDirtyBlockID, i);

					if (GetPageState(bpid) == PageState.Live)
					{
						// Move live pages to reserved block
						BlockPageId pba = new BlockPageId(reserved, index);
						dev.Read(dev.ToUniversalPageId(bpid), buffer);
						dev.Write(dev.ToUniversalPageId(pba), buffer);
						SetPageState(pba, PageState.Live);

						// Update LBA-PBA list
						LogicalId lba = reverseMapList[dev.ToUniversalPageId(bpid)];
						RegisterEntry(lba, pba);

						index++;
					}
					else if (GetPageState(bpid) == PageState.Allc)
					{
						BlockPageId pba = new BlockPageId(reserved, index);
						SetPageState(pba, PageState.Allc);

						// Update LBA-PBA list
						LogicalId lba = reverseMapList[dev.ToUniversalPageId(bpid)];
						RegisterEntry(lba, pba);

						index++;
					}
				}

				// Erase mostDirtyBlockID block
				dev.Erase(mostDirtyBlockID);

				for (ushort i = 0; i < pageCountPerBlock; i++)				
					SetPageState(new BlockPageId(mostDirtyBlockID, i), PageState.Free);

				// Remove mostDirtyBlockID block from dirtyList
				dirtyList.RemoveAt(mostDirtyIndex);

				// Add reserved block into dirtyList
				dirtyList.Add(reserved);

				// Make erased block as reserved block
				reserved = mostDirtyBlockID;
			}

			return true;
		}

		/* Get the data state of specified page */
		private PageState GetPageState(BlockPageId pba)
		{
			return blockStates[pba.BlockId].PageStates[pba.PageId];
		}

		private void SetPageState(BlockPageId pba, PageState ps)
		{
			PageState ori = GetPageState(pba);
			if (ori == ps)
				return;

			switch (ori)
			{
				case PageState.Free:
					blockStates[pba.BlockId].FreePageCount--;
					break;
				case PageState.Allc:
					blockStates[pba.BlockId].AllcPageCount--;
					break;
				case PageState.Live:
					blockStates[pba.BlockId].LivePageCount--;
					break;
				case PageState.Dead:
					blockStates[pba.BlockId].DeadPageCount--;
					break;
			}

			blockStates[pba.BlockId].PageStates[pba.PageId] = ps;

			switch (ps)
			{
				case PageState.Free:
					blockStates[pba.BlockId].FreePageCount++;
					break;
				case PageState.Allc:
					blockStates[pba.BlockId].AllcPageCount++;
					break;
				case PageState.Live:
					blockStates[pba.BlockId].LivePageCount++;
					break;
				case PageState.Dead:
					ushort deadpc = ++blockStates[pba.BlockId].DeadPageCount;
					if (deadpc == dev.PageCountPerBlock)
						MoveDirtyToDead(pba.BlockId);
					break;
			}
		}

		private void MoveDirtyToDead(BlockId blkid)
		{
			bool removalSuccess = dirtyList.Remove(blkid);
			Debug.Assert(removalSuccess);
			deadList.Add(blkid);
		}


	}
}