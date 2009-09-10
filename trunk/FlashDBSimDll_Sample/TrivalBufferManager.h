#ifndef _TRIVAL_BUFFER_MANAGER_H_
#define _TRIVAL_BUFFER_MANAGER_H_

#include <memory>
#include "IBufferManager.h"


class IBlockDevice;

class TrivalBufferManager : public IBufferManager
{
public:
	TrivalBufferManager(std::tr1::shared_ptr<IBlockDevice> pDevice);

	void Read(size_t addr, char *result);
	void Write(size_t addr, const char *data);
	void Flush();
	
	int GetReadCount() const;
	int GetWriteCount() const;

private:
	std::tr1::shared_ptr<IBlockDevice> pdev_;
	int read_, write_;
};

#endif
