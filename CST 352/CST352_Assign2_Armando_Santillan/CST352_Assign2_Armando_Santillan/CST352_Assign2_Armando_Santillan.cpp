// CST352_Assign2_Armando_Santillan.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include "FirstFitPool.h"
#include "StringQueue.h"
#include "BestFitPool.h"

void FirstFitTest();
void BestFitTest();

int main()
{
    FirstFitTest();
    std::cout << std::endl;
    BestFitTest();
}
//test for the firstfitclass
void FirstFitTest() {
    std::cout << "*****************First Fit Test*****************" << std::endl;
    FirstFitPool pool(100);
    StringQueue queue(&pool);
    try {
        std::cout << "Inserting words" << std::endl;
        queue.Insert("hello");
        queue.Insert("hollow world is happenind right now");
        queue.Insert("rod is yellow happening forever");
        queue.Insert("blow");

        std::cout << "First fit removes and inserts on the first available sections" << std::endl;
        std::cout << queue.Peek() << std::endl;
        queue.Remove();
        std::cout << queue.Peek() << std::endl;
        queue.Remove();
        queue.Insert("world");

        std::cout << "Tries to insert, catches OutOfMemory exception" << std::endl;
        std::cout << "Inserting: Rainbows do not bring leprachauns" << std::endl;
        queue.Insert("Inserting: Rainbows do not bring leprachauns");
    }
    catch (OutOfMemory) {
        std::cout << "Out of memory caught" << std::endl;
    }
}
//Tests for the bestfitclass
void BestFitTest() {
    std::cout << "*****************Best Fit Test*****************" << std::endl;
    BestFitPool pool(100);
    StringQueue queue(&pool);
    try {
        std::cout << "Inserting words" << std::endl;
        queue.Insert("hello");
        queue.Insert("hollow world is happenind right now");
        queue.Insert("rod is yellow happening forever");
        queue.Insert("blow");

        std::cout << "Best fit removes and inserts on the smallest available section" << std::endl;
        std::cout << queue.Peek() << std::endl;
        queue.Remove();
        std::cout << queue.Peek() << std::endl;
        queue.Remove();
        queue.Insert("world");

        std::cout << "Tries to insert, catches OutOfMemory exception" << std::endl;
        std::cout << "Inserting: Dragons are very magical creatures" << std::endl;
        queue.Insert("Inserting: Dragons are very magical creatures");
    }
    catch (OutOfMemory) {
        std::cout << "Out of memory caught" << std::endl;
    }
}


