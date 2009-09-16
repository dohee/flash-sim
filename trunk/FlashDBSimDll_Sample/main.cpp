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
	int bufferSize = 100;

	shared_ptr<IBlockDevice> pdev(new TrivalBlockDevice());
	shared_ptr<IBlockDevice> pdevCFLRU(new TrivalBlockDevice());
	shared_ptr<IBlockDevice> pdevLRUWSR(new TrivalBlockDevice());
	//shared_ptr<IBufferManager> pmgr(new TrivalBufferManager(pdev));
	shared_ptr<IBufferManager> pmgr(new LRUBufferManager(pdev, bufferSize));
	shared_ptr<IBufferManager> pmgrCFLRU(new CFLRUBufferManager(pdevCFLRU, bufferSize, bufferSize/2));
	shared_ptr<IBufferManager> pmgrLRUWSR(new LRUWSRBufferManager(pdevLRUWSR, bufferSize, 1));

	srand(clock());
	int fcount = 0;
	ifstream traceFile("trace.txt");

	if(!traceFile.is_open())
	{
		cout<<"file missing~~"<<endl;
		exit(1);
	}

	int count=0;

	while (!traceFile.eof())
	{
		if(++count % 2000 == 0)
		{
			cout<<count<<endl;
		}
		size_t pageid;
		int length;
		int rw;
		char buf[2048];

		traceFile >> pageid >> length >> rw;
		rw = rand() % 2;

		if (count < 0)
			continue;
		if (count > 10000)
			break;
		
		//cout<<pageid<<","<<rw<<endl;
		for(int i = 0; i<length; i++)
		{
			if (rw == 0) {
				pmgr->Read(pageid, buf);
				pmgrCFLRU->Read(pageid, buf);
				pmgrLRUWSR->Read(pageid, buf);
			} else {
				pmgr->Write(pageid, buf);
				pmgrCFLRU->Write(pageid, buf);
				pmgrLRUWSR->Write(pageid, buf);
			}

			pageid++;
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
