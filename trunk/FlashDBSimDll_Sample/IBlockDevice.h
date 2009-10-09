#ifndef _I_BLOCK_DEVICE_H_
#define _I_BLOCK_DEVICE_H_
#pragma managed(push, off)

extern const int WRITECOST;
extern const int READCOST;


class IBlockDevice abstract
{
public:
	virtual size_t GetPageSize() const = 0;

	virtual void Read(size_t pageid, void *result) = 0;
	virtual void Write(size_t pageid, const void *data) = 0;

	virtual int GetReadCount() const = 0;
	virtual int GetWriteCount() const = 0;
	virtual int GetTotalCost() const = 0;

};

#pragma managed(pop)
#endif
