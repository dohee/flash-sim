#include "stdafx.h"
#include <ctime>
#include "TrivalBlockDevice.h"
#include "TrivalBufferManager.h"
#include "LRUBufferManager.h"
#include "CFLRUBufferManager.h"
#include "LRUWSRBufferManager.h"
using namespace std;
using namespace std::tr1;

void main()
{
	int bufferSize = 5000;

	shared_ptr<IBlockDevice> pdev(new TrivalBlockDevice(2048));
	shared_ptr<IBlockDevice> pdevCFLRU(new TrivalBlockDevice(2048));
	shared_ptr<IBlockDevice> pdevLRUWSR(new TrivalBlockDevice(2048));
	//shared_ptr<IBufferManager> pmgr(new TrivalBufferManager(pdev));
	shared_ptr<IBufferManager> pmgr(new LRUBufferManager(pdev, bufferSize));
	shared_ptr<IBufferManager> pmgrCFLRU(new CFLRUBufferManager(pdevCFLRU, bufferSize, bufferSize/2));
	shared_ptr<IBufferManager> pmgrLRUWSR(new LRUWSRBufferManager(pdevLRUWSR, bufferSize, 1));

	int fcount = 0;
	ifstream traceFile("trace.txt");
	if(!traceFile.is_open())
	{
		cout<<"file missing~~"<<endl;
		exit(1);
	}
	srand(clock());

	int count=0;

	while (!traceFile.eof())
	{
		if(++count % 2000 == 0)
		{
			cout<<count<<endl;
		}
		size_t addr;
		int length;
		int rw;
		char buf[2048];

		traceFile >> addr >> length >> rw;
		rw = rand() % 2;

		if (count < 160000)
			continue;
		if (count > 180000)
			continue;
		
		//cout<<addr<<","<<rw<<endl;
		for(int i = 0; i<length; i++)
		{
			if (rw == 0)
			{
				pmgr->Read(addr, buf);
				pmgrCFLRU->Read(addr, buf);
				pmgrLRUWSR->Read(addr, buf);
			}
			else{
				pmgr->Write(addr, buf);
				pmgrCFLRU->Write(addr, buf);
				pmgrLRUWSR->Write(addr, buf);
			}
			addr++;
		}
	}

	pmgr->Flush();
	pmgrCFLRU->Flush();
	pmgrLRUWSR->Flush();

	cout<< "LRU" <<endl
		<< pmgr->GetReadCount() << endl
		<< pmgr->GetWriteCount() << endl
		<< pdev->GetReadCount() << endl
		<< pdev->GetWriteCount() << endl
		<< pdev->GetTotalCost() << endl
		<< endl;

	cout<< "CFLRU" <<endl
		<< pmgrCFLRU->GetReadCount() << endl
		<< pmgrCFLRU->GetWriteCount() << endl
		<< pdevCFLRU->GetReadCount() << endl
		<< pdevCFLRU->GetWriteCount() << endl
		<< pdevCFLRU->GetTotalCost() << endl
		<< endl;

	cout<< "LRUWSR" <<endl
		<< pmgrLRUWSR->GetReadCount() << endl
		<< pmgrLRUWSR->GetWriteCount() << endl
		<< pdevLRUWSR->GetReadCount() << endl
		<< pdevLRUWSR->GetWriteCount() << endl
		<< pdevLRUWSR->GetTotalCost() << endl
		<<endl;
}
