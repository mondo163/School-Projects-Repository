#include "MemoryPool.h"

//constructor and destructor for the memory pool
MemoryPool::MemoryPool(unsigned int poolSize) {
	pool = new unsigned char[poolSize];

	chunks.emplace_back(Chunk(0, poolSize, false));
};
MemoryPool::~MemoryPool() {
	delete[] pool;
}

void* MemoryPool::Allocate(unsigned int nBytes) {
	//find first available chunk that is big enough to fit nbytes
	auto iter = FindAvailableChunk(nBytes);
	

	//can't find an available chunk that fits...
	if (iter == chunks.end())
	{
		throw OutOfMemory();
	}

	//split it if necessary
	if (iter->size > nBytes)
	{
		unsigned int start = iter->startingIndex;
		//resize the original chunk
		iter->size -= nBytes;
		iter->startingIndex += nBytes;
		//split! add new chunk for cllocated bytes
		iter = chunks.emplace(iter, Chunk(start, nBytes, true));
		
	}

	//indicate that the chunk is allocated
	iter->allocated = true;
	
	return pool + iter->startingIndex;
}

void MemoryPool::Free(void* block) {
	//find the chunk allocated for the block
	//loop through chunkc, find the allocated one and starting index == block-pool
	auto iter = chunks.begin();
	while (iter != chunks.end() && (!iter->allocated || iter->startingIndex != ((unsigned char*)block - pool)))
	{
		//not the one! keep looking
		iter++;
	}

	//if we find the missing chunk
	if (iter != chunks.end())
	{
		iter->allocated = false;
		//wont check previous chunk if its at the beginning
		if (iter != chunks.begin())
		{
			//coallesce the current chunk with the previous chunk
			auto previous = std::prev(iter);
			if (!previous->allocated)
			{
				previous->size += iter->size;
				chunks.erase(iter);
				iter = previous;
			}
		}
		
		auto next = std::next(iter);
		if (next != chunks.end())
		{
			
			if (!next->allocated)
			{
				//coallesce with chunk after this one
				//update this chunks size to include the next chunk
				//erase next chunk
				iter->size += next->size;
				chunks.erase(next);
			}
		}
		
		
	}
}

void MemoryPool::DebugPrint() {

	std::cout << " MemoryPool ->" << std::endl;
	for (const Chunk& chunk : chunks) {
		std::cout << "\t" << chunk.startingIndex
			<< ", " << chunk.size
			<< ", " << chunk.allocated
			<< std::endl;
	}
	std::cout << "<-" << std::endl;

}