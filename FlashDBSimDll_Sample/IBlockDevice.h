#ifndef _I_BLOCK_DEVICE_H_
#define _I_BLOCK_DEVICE_H_

const int WRITECOST = 200;
const int READCOST = 80;

class IBlockDevice abstract
{
public:
	virtual size_t GetPageSize() const = 0;

	virtual void Read(size_t addr, void *result) = 0;
	virtual void Write(size_t addr, const void *data) = 0;

	virtual int GetReadCount() const = 0;
	virtual int GetWriteCount() const = 0;
	virtual int GetTotalCost() const = 0;

};

#endif
