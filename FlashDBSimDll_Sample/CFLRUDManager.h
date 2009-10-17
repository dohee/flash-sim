#ifndef _CFLRUDD_BUFFER_MANAGER_H_
#define _CFLRUDD_BUFFER_MANAGER_H_
#pragma managed(push, off)

#include <memory>
#include <list>
#include <hash_map>
#include "BufferManagerBase.h"
#include "TrivalBlockDevice.h"


class CFLRUDManager : public BufferManagerBase
{
public:
	CFLRUDManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages, size_t iwindowSize = 0);
	virtual ~CFLRUDManager();

protected:
	virtual void DoRead(size_t pageid, void *result);
	virtual void DoWrite(size_t pageid, const void *data);
	virtual void DoFlush();

private:
	void setwindowSize(size_t iwindowSize);
	std::tr1::shared_ptr<struct DataFrame> AccessFrame_(size_t pageid);
	std::tr1::shared_ptr<struct DataFrame> AcquireFrame_(size_t pageid);
	void AcquireSlot_();
	void WriteIfDirty(std::tr1::shared_ptr<struct DataFrame> pFrame);

private:
	size_t windowSize_;		//windowSize parameter of CFLRUD
	std::list<int> observingList_;		//remain passed access for a while
	size_t observingNum_;		//Limit length of observingList
	size_t totalCost_;		//Total cost of observingList
	
	typedef std::list<std::tr1::shared_ptr<struct DataFrame> > QueueType;
	typedef stdext::hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;

};

#pragma managed(pop)
#endif