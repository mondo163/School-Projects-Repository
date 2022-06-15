#include "StringQueue.h"
StringQueue::StringQueue(MemoryPool* pool):pool(pool){}
//inserts a string in the pool and adds to the queue
void StringQueue::Insert(const char* str) {
	size_t len = strlen(str) + 1;
	char* pstr = (char*)pool->Allocate(len);
	strcpy_s(pstr, len, str);
	theQueue.push(pstr);
	pool->DebugPrint();
}
//returns the word at the front of the queue
const char* StringQueue::Peek() const {
	return theQueue.front();
}
//removes the word at the front of the queue and from the pool
void StringQueue::Remove() {
	
	char* pstr = theQueue.front();
	pool->Free(pstr);
	pool->DebugPrint();
	theQueue.pop();
}