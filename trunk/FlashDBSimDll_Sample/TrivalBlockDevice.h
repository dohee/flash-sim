#ifndef _TRIVAL_BLOCK_DEVICE_
#define _TRIVAL_BLOCK_DEVICE_
#pragma managed(push, off)

#include "IBlockDevice.h"

class TrivalBlockDevice : public IBlockDevice
{
public:
	TrivalBlockDevice();
	size_t GetPageSize() const;

	void Read(size_t pageid, void *result);
	void Write(size_t pageid, const void *data);

	int GetReadCount() const;
	int GetWriteCount() const;
	int GetTotalCost() const;

private:
	int read_, write_;
};

#pragma managed(pop)
#endif
