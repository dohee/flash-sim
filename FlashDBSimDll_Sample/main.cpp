#include "stdafx.h"
#include "TrivalBlockDevice.h"
#include "TrivalBufferManager.h"
#include "LRUBufferManager.h"
using namespace std;
using namespace std::tr1;

void main()
{
	shared_ptr<IBlockDevice> pdev(new TrivalBlockDevice(2048));
	//shared_ptr<IBufferManager> pmgr(new TrivalBufferManager(pdev));
	shared_ptr<IBufferManager> pmgr(new LRUBufferManager(pdev, 10000));

	FILE *fp = NULL;
	if ((fp = fopen("trace1000000","r")) == NULL)
	{
		printf(" cannot open trace file\n");
		return;
	}

	int fcount = 0;
	while (fcount++ < 10000)
	{
		size_t addr;
		int rw;
		char buf[2048];
		fscanf(fp, "%d %d", &addr, &rw);

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