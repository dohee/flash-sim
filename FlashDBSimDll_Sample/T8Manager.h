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

	static bool FindInQueue_(size_t pageid, QueueType& queue, QueueType::iterator& out);
	static void AdjustQueue_(QueueType& queue, QueueType::iterator iter);
	void AcquireSlot_();

private:
	QueueType qclean_, qdirty_;
	QueueType cleanResident_, cleanNonresident_;
	QueueType dirtyResident_, dirtyNonresident_;
};

#endif