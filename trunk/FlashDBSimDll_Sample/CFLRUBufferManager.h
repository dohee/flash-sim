#ifndef _CFLRU_BUFFER_MANAGER_H_
#define _CFLRU_BUFFER_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "BufferManagerBase.h"


class CFLRUBufferManager : public BufferManagerBase
{
public:
	CFLRUBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages, size_t iwindowSize);
	virtual ~CFLRUBufferManager();

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
	size_t windowSize;		//windowSize parameter of CFLRU
	
	typedef std::list<std::tr1::shared_ptr<struct DataFrame> > QueueType;
	typedef stdext::hash_map<size_t, QueueType::iterator> MapType;
	QueueType queue_;
	MapType map_;

};

#endif