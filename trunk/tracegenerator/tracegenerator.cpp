// tracegenerator.cpp : �������̨Ӧ�ó������ڵ㡣
//

#include "stdafx.h"
#include "ZipfGenerator.h"


int _tmain(int argc, _TCHAR* argv[])
{
	size_t pageNumber = 1000;
	ZipfGenerator zipfGenerator(pageNumber,0.8);

	int counter = 0;
	for(int i =0; i<100000; i++)
	{
		size_t address = zipfGenerator.get();
		int size = 1;

		//int dirty = (int)((address>400)&&(((double)rand()/ RAND_MAX<0.75));// || ((address<500)&&((double)rand()/ RAND_MAX<0.1)));
		int dirty = (int)(rand()%8<address%8)/2;//*(int)((double)rand()/ RAND_MAX  < (double)address/pageNumber);
		cout<<address<<"\t"<<size<<"\t"<<dirty<<endl;
		if (address<200) counter++;
	}
	//cout<<counter;
}