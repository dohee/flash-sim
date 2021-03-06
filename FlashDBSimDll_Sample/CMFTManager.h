#ifndef _CMFT_BUFFER_MANAGER_H_
#define _CMFT_BUFFER_MANAGER_H_
#pragma managed(push, off)

#include <memory>
#include <deque>
#include <hash_map>
#include "FrameBasedBufferManager.h"

class CMFTManager : public FrameBasedBufferManager
{
public:
	CMFTManager(std::tr1::shared_ptr<class IBlockDevice> pDevice, size_t nPages);
	virtual ~CMFTManager();

protected:
	void DoFlush();
	std::tr1::shared_ptr<struct DataFrame> FindFrame(size_t pageid, bool isWrite);
	std::tr1::shared_ptr<struct DataFrame> AllocFrame(size_t pageid, bool isWrite);

private:

private:
	size_t time_;
	typedef std::deque<std::tr1::shared_ptr<struct CMFTFrame> > StackType;
	StackType stack_;
};

#pragma managed(pop)
#endif