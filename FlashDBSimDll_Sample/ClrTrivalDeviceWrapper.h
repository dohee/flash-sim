#ifndef _CLR_TRIVAL_DEVICE_WRAPPER_H_
#define _CLR_TRIVAL_DEVICE_WRAPPER_H_

#pragma managed(push, off)
#include "IBlockDevice.h"
#pragma managed(on)
#include <vcclr.h>

class ClrTrivalDeviceWrapper : public IBlockDevice
{
public:
	ClrTrivalDeviceWrapper();
	ClrTrivalDeviceWrapper(Buffers::IBlockDevice^ pdevice);
	size_t GetPageSize() const;

	void Read(size_t pageid, void *result);
	void Write(size_t pageid, const void *data);

	int GetReadCount() const;
	int GetWriteCount() const;
	int GetTotalCost() const;

private:
	gcroot<Buffers::IBlockDevice^> pdev;
};

#pragma managed(pop)
#endif