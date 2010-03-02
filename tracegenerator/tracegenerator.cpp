// tracegenerator.cpp : 定义控制台应用程序的入口点。
//

#include "stdafx.h"
#include "ZipfGenerator.h"

double getRandom()
{
	return (double)rand()/ RAND_MAX;
}


int _tmain(int argc, _TCHAR* argv[])
{
	size_t pageNumber = 1000;
	ZipfGenerator zipfGenerator(pageNumber,0.8);

	int counter = 0;
	for(int i =0; i<100000; i++)
	{
		size_t address = zipfGenerator.get();
		int size = 1;
		int dirty =1;

		//int dirty = (int)((address>400)&&(((double)rand()/ RAND_MAX<0.75));// || ((address<500)&&((double)rand()/ RAND_MAX<0.1)));
		//int dirty = (int)(rand()%8<address%8)/2;//*(int)((double)rand()/ RAND_MAX  < (double)address/pageNumber);
		if(getRandom()<0.5)
		{
			dirty=0;
			address=(int)(getRandom()*200);
		}
		else
		{
			dirty=1;
			address=(int)(getRandom()*300)+200;
		}
			

		cout<<address<<"\t"<<size<<"\t"<<dirty<<endl;
		if (address<200) counter++;
	}
	//cout<<counter;
}

