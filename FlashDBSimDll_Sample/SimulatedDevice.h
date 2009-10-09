#ifndef _SIMULATED_DEVICE_H_
#define _SIMULATED_DEVICE_H_
#pragma managed(push, off)

#include <memory>
#include "IBlockDevice.h"

class SimulatedDevice : public IBlockDevice
{
public:
	static std::tr1::shared_ptr<SimulatedDevice> Singleton();

	virtual size_t GetPageSize() const;

	virtual void Read(size_t pageid, void *result);
	virtual void Write(size_t pageid, const void *data);

	virtual int GetReadCount() const;
	virtual int GetWriteCount() const;
	virtual int GetTotalCost() const;

private:
	SimulatedDevice();
};

#pragma managed(pop)
#endif
