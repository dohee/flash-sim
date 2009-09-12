#include "stdafx.h"
#include <ctime>
#include "TrivalBlockDevice.h"
#include "TrivalBufferManager.h"
#include "LRUBufferManager.h"
using namespace std;
using namespace std::tr1;

void main()
{
	shared_ptr<IBlockDevice> pdev(new TrivalBlockDevice(2048));
	//shared_ptr<IBufferManager> pmgr(new TrivalBufferManager(pdev));
	shared_ptr<IBufferManager> pmgr(new LRUBufferManager(pdev, 1000));

	int fcount = 0;
	srand(clock());

	while (fcount++ < 10000)
	{
		size_t addr = rand();
		int rw = rand() % 10;
		char buf[2048];

		if (rw == 0)
			pmgr->Read(addr * 512, buf);
		else
			pmgr->Write(addr * 512, buf);
	}

	pmgr->Flush();

	cout << pmgr->GetReadCount() << endl
		<< pmgr->GetWriteCount() << endl
		<< pdev->GetReadCount() << endl
		<< pdev->GetWriteCount() << endl;
}