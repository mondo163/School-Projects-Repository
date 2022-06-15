//MemoryPool.h
//Armando Santillan
//CST352 Spring 2022
#include <vector>
#include <iostream>
#include <iterator>
#pragma once

class OutOfMemory {};

class MemoryPool
{
protected:
	class Chunk {
	public:
		unsigned int startingIndex;
		unsigned int size;
		bool allocated;

		Chunk(unsigned int startingIndex, unsigned int size, bool allocated) :
			startingIndex(startingIndex), size(size), allocated(allocated) {}
	};
	
	std::vector<Chunk> chunks;
	unsigned char* pool;
	

public:
	MemoryPool(unsigned int poolSize);
	virtual ~MemoryPool();

	virtual void* Allocate(unsigned int nBytes);

	virtual void Free(void* block);

	virtual void DebugPrint();

protected:
	virtual std::vector<Chunk>::iterator FindAvailableChunk(unsigned int nBytes) = 0;
};

