#ifndef _GENERATOR_
#define _GENERATOR_
#pragma once
//产生随机访问的地址。

class Generator
{
public:

	~Generator(void);
	Generator();
	Generator(size_t iaddressNum);
	virtual size_t get() = 0;		//得到一个地址

	int addressNum;				//可以访问的地址总数
};

#endif