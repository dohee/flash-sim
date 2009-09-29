#ifndef _TRIVAL_BLOCK_DEVICE_
#define _TRIVAL_BLOCK_DEVICE_

#include "IBlockDevice.h"

const int WRITECOST = 200;
const int READCOST = 66;

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

#endif
