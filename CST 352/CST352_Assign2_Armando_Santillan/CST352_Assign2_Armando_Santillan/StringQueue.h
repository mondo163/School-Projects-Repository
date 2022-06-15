#pragma once
#include "MemoryPool.h"
#include <queue>

class StringQueue
{
private:
	MemoryPool* pool;
	std::queue<char*> theQueue;

public:
	StringQueue(MemoryPool* pool);
	void Insert(const char* str);
	const char* Peek() const;
	void Remove();
};

