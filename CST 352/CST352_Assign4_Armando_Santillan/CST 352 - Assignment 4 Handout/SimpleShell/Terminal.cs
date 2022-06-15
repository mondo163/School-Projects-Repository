// Assignment 4
// Pete Myers
// OIT, Spring 2018
// Handout

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace SimpleShell
{
    public class Terminal
    {
        // NOTE: performs line discipline over driver
        private TerminalDriver driver;
        private LineQueue completedLineQueue;
        private Handler handler;

        public Terminal(TerminalDriver driver)
        {
            completedLineQueue = new LineQueue();
            handler = new Handler(driver, completedLineQueue);

            this.driver = driver;
            this.driver.InstallInterruptHandler(handler);
        }

        public void Connect()
        {
            driver.Connect();
        }

        public void Disconnect()
        {
            driver.Disconnect();
        }

        public bool Echo { get { return handler.Echo; } set { handler.Echo = value; } }

        public string ReadLine()
        {
            
            return completedLineQueue.Remove();
        }

        public void Write(string line)
        {
            foreach (char item in line)
            {
                driver.SendChar(item);
            }
        }

        public void WriteLine(string line)
        {
            //Send the line of charactesr followed by new line
            Write(line);
            driver.SendNewLine();
        }

        private class LineQueue
        {
            private Queue<string> theQueue;
            private Mutex mutex;
            private ManualResetEvent hasItemsEvent;

            public LineQueue()
            {
                this.theQueue = new Queue<string>();
                this.mutex = new Mutex();
                this.hasItemsEvent = new ManualResetEvent(false);   // initially is empty
            }

            public void Insert(string s)
            {
                // wait until  we have the mutex
                mutex.WaitOne();
                // insert into the buffer
                theQueue.Enqueue(s);
                // signal any threads waiting to remove an object
                hasItemsEvent.Set();
                //release mutex
                mutex.ReleaseMutex();
            }

            public string Remove()
            {
                WaitHandle.WaitAll(new WaitHandle[] { this.mutex, hasItemsEvent });
                // remove the item from the buffer
                string line = theQueue.Dequeue();
                // block any threads waiting to remove, if the queue is empty
                if (theQueue.Count == 0)
                    hasItemsEvent.Reset();
                //release the mutex
                mutex.ReleaseMutex();
                return line;
            }

            public int Count()
            {
                // wait until we have the mutex
                mutex.WaitOne();
                // return the number of items in the queue
                int count = theQueue.Count;

                //release the mutex
                mutex.ReleaseMutex();
                return count;
            }
        }

        class Handler : TerminalInterruptHandler
        {
            private TerminalDriver driver;
            private List<char> partialLineQueue;
            private LineQueue completedLineQueue;

            public Handler(TerminalDriver driver, LineQueue completedLineQueue)
            {
                this.driver = driver;
                this.completedLineQueue = completedLineQueue;
                this.partialLineQueue = new List<char>();
            }

            public bool Echo { get; set; }

            public void HandleInterrupt(TerminalInterrupt interrupt)
            {
                switch (interrupt)
                {
                    case TerminalInterrupt.CHAR:
                        // queue up the characters until we have a completed line
                        char c = driver.RecvChar();
                        partialLineQueue.Add(c);
                        //if echo is on, send the character back
                        if (Echo)
                            driver.SendChar(c);
                        break;

                    case TerminalInterrupt.ENTER:
                        // get all the characters from the partial line queue and create a completed line
                        string line = new string(partialLineQueue.ToArray());
                        partialLineQueue.Clear();
                        completedLineQueue.Insert(line);
                        // if echo on, send line
                        if (Echo)
                            driver.SendNewLine();
                        break;

                    case TerminalInterrupt.BACK:
                        // throw away the last character entered
                        if (partialLineQueue.Count > 0) // will only do it if the partial line queue contains characters. 
                       { 
                            partialLineQueue.RemoveAt(partialLineQueue.Count - 1);
                            //if echo is on, remove the characters from the terminal
                            if (Echo)
                            {
                                driver.SendChar((char)8); //back space
                                driver.SendChar((char)32); //space
                                driver.SendChar((char)8); //backspace
                            }
                        }
                        break;
                }
            }
        }
    }
}
