#ifndef _GENERATOR_
#define _GENERATOR_
#pragma once
//����������ʵĵ�ַ��

class Generator
{
public:

	~Generator(void);
	Generator();
	Generator(size_t iaddressNum);
	virtual size_t get() = 0;		//�õ�һ����ַ

	int addressNum;				//���Է��ʵĵ�ַ����
};

#endif