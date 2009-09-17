#ifndef _BUFFER_MANAGER_GROUP_
#define _BUFFER_MANAGER_GROUP_

#include <memory>
#include <vector>
#include "IBufferManager.h"

class BufferManagerGroup : public IBufferManager
{
public:
	void Add(std::tr1::shared_ptr<class BufferManagerBase> pbuf);

	void Read(size_t pageid, void *result);
	void Write(size_t pageid, const void *data);
	void Flush();
	
	int GetMgrCount() const;
	int GetReadCount() const;
	int GetWriteCount() const;
	int GetDevReadCount(size_t index) const;
	int GetDevWriteCount(size_t index) const;
	int GetDevCost(size_t index) const;

private:
	typedef std::vector<std::tr1::shared_ptr<class BufferManagerBase> > MgrsType;
	MgrsType mgrs_;
};


#endif