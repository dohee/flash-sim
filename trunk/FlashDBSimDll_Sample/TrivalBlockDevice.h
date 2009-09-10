#ifndef _TRIVAL_BLOCK_DEVICE_
#define _TRIVAL_BLOCK_DEVICE_

#include "IBlockDevice.h"

class TrivalBlockDevice : public IBlockDevice
{
public:
	TrivalBlockDevice(size_t pageSize);
	void Read(size_t addr, char *result);
	void Write(size_t addr, const char *data);

private:
	size_t pageSize_;
};

#endif
