#include "stdafx.h"
#include <ctime>
#include "TrivalBlockDevice.h"
#include "TrivalBufferManager.h"
#include "LRUBufferManager.h"
#include "CFLRUBufferManager.h"
using namespace std;
using namespace std::tr1;

void main()
{
	shared_ptr<IBlockDevice> pdev(new TrivalBlockDevice(2048));
	shared_ptr<IBlockDevice> pdevCFLRU(new TrivalBlockDevice(2048));
	//shared_ptr<IBufferManager> pmgr(new TrivalBufferManager(pdev));
	shared_ptr<IBufferManager> pmgr(new LRUBufferManager(pdev, 1000));
	shared_ptr<IBufferManager> pmgrCFLRU(new CFLRUBufferManager(pdevCFLRU, 1000, 1000/2));

	int fcount = 0;
	srand(clock());

	while (fcount++ < 10000)
	{
		size_t addr = rand()/10;
		int rw = rand() % 3;
		char buf[2048];

		if (rw == 0)
		{
			pmgr->Read(addr * 512, buf);
			pmgrCFLRU->Read(addr * 512, buf);
		}
		else{
			pmgr->Write(addr * 512, buf);
			pmgrCFLRU->Write(addr * 512, buf);
		}
	}

	pmgr->Flush();
	pmgrCFLRU->Flush();

	cout<< "LRU" <<endl
		<< pmgr->GetReadCount() << endl
		<< pmgr->GetWriteCount() << endl
		<< pdev->GetReadCount() << endl
		<< pdev->GetWriteCount() << endl <<endl;

	cout<< "CFLRU" <<endl
		<< pmgrCFLRU->GetReadCount() << endl
		<< pmgrCFLRU->GetWriteCount() << endl
		<< pdevCFLRU->GetReadCount() << endl
		<< pdevCFLRU->GetWriteCount() << endl;
}