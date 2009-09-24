#ifndef _T8_MANAGER_H_
#define _T8_MANAGER_H_

#include <memory>
#include <list>
#include <hash_map>
#include "BufferManagerBase.h"

class T8Manager : public BufferManagerBase
{
public:
	T8Manager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~T8Manager();

protected:
	virtual void DoRead(size_t pageid, void *result);
	virtual void DoWrite(size_t pageid, const void *data);
	virtual void DoFlush();

private:
	typedef std::list<std::tr1::shared_ptr<struct DataFrame> > QueueType;
	enum QueueIndex { NONE = 0, CR, CNR, DR, DNR, COUNT };

	bool FindInQueue_(QueueIndex index, size_t pageid, QueueType::iterator& out);
	QueueIndex FindInQueues_(size_t pageid, QueueType::iterator& out);

	void SetLimits_(size_t cleanResidentSize);
	void EnlargeCRLimit_(int relativeCRSize);
	void AdjustQueue_(QueueIndex queue, QueueType::iterator iter);
	std::tr1::shared_ptr<struct DataFrame> PushIntoQueue_(QueueIndex queue, std::tr1::shared_ptr<struct DataFrame> pframe);
	std::tr1::shared_ptr<struct DataFrame> PushIntoQueues_(QueueIndex headqueue, std::tr1::shared_ptr<struct DataFrame> pframe);
	void WriteIfDirty(struct DataFrame& frame);

private:
	QueueType q_[COUNT];
	size_t limits_[COUNT];
};

#endif