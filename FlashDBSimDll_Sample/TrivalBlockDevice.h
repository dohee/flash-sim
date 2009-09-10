#ifndef _TRIVAL_BLOCK_DEVICE_
#define _TRIVAL_BLOCK_DEVICE_

#include "IBlockDevice.h"

class TrivalBlockDevice : public IBlockDevice
{
public:
	TrivalBlockDevice(size_t pageSize);
	size_t GetPageSize() const;

	void Read(size_t addr, void *result);
	void Write(size_t addr, const void *data);

	int GetReadCount() const;
	int GetWriteCount() const;

private:
	size_t pageSize_;
	int read_, write_;
};

#endif
