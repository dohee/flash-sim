#include "stdafx.h"
#include "CMFTBufferManager.h"
#include "IBlockDevice.h"
#include "frame.h"
using namespace std;
using namespace stdext;
using namespace std::tr1;



class CMFTBufferManagerImpl
{
public:
	CMFTBufferManagerImpl(shared_ptr<IBlockDevice> pDevice, size_t nPages) {}
	void Read(size_t pageid, void *result) {}
	void Write(size_t pageid, const void *data){}
	void Flush() {}
private:

private:

};






CMFTBufferManager::CMFTBufferManager(shared_ptr<IBlockDevice> pDevice, size_t nPages)
: pImpl(new CMFTBufferManagerImpl(pDevice, nPages))
{ }

CMFTBufferManager::~CMFTBufferManager()
{
	Flush();
}

void CMFTBufferManager::DoRead(size_t pageid, void *result)
{
	pImpl->Read(pageid, result);
}
void CMFTBufferManager::DoWrite(size_t pageid, const void *data)
{
	pImpl->Write(pageid, data);
}
void CMFTBufferManager::DoFlush()
{
	pImpl->Flush();
}
