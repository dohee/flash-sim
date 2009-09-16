#ifndef _CMFT_BUFFER_MANAGER_H_
#define _CMFT_BUFFER_MANAGER_H_

#include <memory>
#include <deque>
#include <hash_map>
#include "FrameBasedBufferManager.h"

class CMFTBufferManager : public FrameBasedBufferManager
{
public:
	CMFTBufferManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~CMFTBufferManager();

protected:
	void DoFlush();
	std::tr1::shared_ptr<struct Frame> FindFrame(size_t pageid);
	std::tr1::shared_ptr<struct Frame> AllocFrame(size_t pageid);

private:

private:
	size_t time_;
	typedef std::deque<std::tr1::shared_ptr<struct CMFTFrame> > StackType;
	StackType stack_;
};

#endif