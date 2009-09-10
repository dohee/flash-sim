#ifndef _SIMULATED_DEVICE_H_
#define _SIMULATED_DEVICE_H_

#include <memory>
#include "IBlockDevice.h"

class SimulatedDevice : public IBlockDevice
{
public:
	static std::tr1::shared_ptr<SimulatedDevice> Singleton();

	virtual size_t GetPageSize() const;

	virtual void Read(size_t addr, void *result);
	virtual void Write(size_t addr, const void *data);

	virtual int GetReadCount() const;
	virtual int GetWriteCount() const;

private:
	SimulatedDevice();
};

#endif
