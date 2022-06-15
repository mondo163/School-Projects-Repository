// SimpleFS.cs
// Pete Myers
// Spring 2018-2020
//
// NOTE: Implement the methods and classes in this file
//
//Armando Santillan - 5/21/22
//
using System;
using System.Collections.Generic;
using System.Linq;


namespace SimpleFileSystem
{
    public class SimpleFS : FileSystem
    {
        #region filesystem

        //
        // File System
        //

        private const char PATH_SEPARATOR = FSConstants.PATH_SEPARATOR;
        private const int MAX_FILE_NAME = FSConstants.MAX_FILENAME;
        private const int BLOCK_SIZE = 500;     // 500 bytes... 2 sectors of 256 bytes each (minus sector overhead)

        private VirtualFS virtualFileSystem;

        public SimpleFS()
        {
            virtualFileSystem = new VirtualFS();
        }

        public void Mount(DiskDriver disk, string mountPoint)
        {
            virtualFileSystem.Mount(disk, mountPoint);
        }

        public void Unmount(string mountPoint)
        {
            virtualFileSystem?.Unmount(mountPoint);
        }

        public void Format(DiskDriver disk)
        {
            virtualFileSystem.Format(disk);
        }

        public Directory GetRootDirectory()
        {
            //get access to the virtual file systems root node
            //wrap the root node in a directory and return it
            return new SimpleDirectory(virtualFileSystem.RootNode);
        }

        public FSEntry Find(string path)
        {
            // good:  /foo/bar, /foo/bar/
            // bad:  foo, foo/bar, //foo/bar, /foo//bar, /foo/../foo/bar

            //validate the path
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Can't find with and empty path!");
            if (!path.StartsWith(PathSeparator.ToString()))
                throw new Exception("Path must be fully qualified!");

            //if they're looking for the root node, just return it!
            if (path == PathSeparator.ToString())
                return new SimpleDirectory(virtualFileSystem.RootNode);

            //if path has a trailing / remove it
            if (path.EndsWith(PathSeparator.ToString()))
                path = path.TrimEnd(PATH_SEPARATOR);

            //split path around the path seperators
            string[] parts = path.Split(PATH_SEPARATOR);

            //descend starting at root finding children as we go
            VirtualNode childNode = virtualFileSystem.RootNode;
            foreach (string part in parts.Skip(1))
            {
                //try to descend
                childNode = childNode.GetChild(part);

                if (childNode == null)
                    return null;
            }  

            //return either a directory or a file object
            return childNode.IsDirectory ? new SimpleDirectory(childNode) as FSEntry :new SimpleFile(childNode) as FSEntry;
        }

        public char PathSeparator { get { return PATH_SEPARATOR; } }
        public int MaxNameLength { get { return MAX_FILE_NAME; } }

        #endregion

        #region implementation

        //
        // FSEntry
        //

        abstract private class SimpleEntry : FSEntry
        {
            protected VirtualNode node;

            protected SimpleEntry(VirtualNode node)
            {
                this.node = node;
            }

            public string Name => node.Name;
            public Directory Parent => node.Parent == null ? null : new SimpleDirectory(node.Parent);

            public string FullPathName
            {
                get
                {
                    string path = "";

                    VirtualNode parent = node.Parent;
                    while (parent != null)
                    {
                        
                        //prepend seperator if needed
                        if (parent.Name != PATH_SEPARATOR.ToString())
                            path = PATH_SEPARATOR+path;

                        //prepend parent's name
                        path = parent.Name + path;

                        //continue toward the root..
                        parent = parent.Parent;
                    }
                    path = Name;

                    return path;
                }
            }

            // override in derived classes
            public virtual bool IsDirectory => node.IsDirectory;
            public virtual bool IsFile => node.IsFile;

            public void Rename(string name)
            {
                // TODO: SimpleEntry.Rename()
            }

            public void Move(Directory destination)
            {
                // TODO: SimpleEntry.Move()
            }

            public void Delete()
            {
                // TODO: SimpleEntry.Delete()
            }
        }

        //
        // Directory
        //

        private class SimpleDirectory : SimpleEntry, Directory
        {
            public SimpleDirectory(VirtualNode node) : base(node)
            {
            }

            public IEnumerable<Directory> GetSubDirectories()
            {
                //get list of children, filter through only directories, and select them as new SimpleDirectories
                return node.GetChildren()
                           .Where(c => c.IsDirectory == true)
                           .Select<VirtualNode, Directory>(c => new SimpleDirectory(c))
                           .ToList();
            }

            public IEnumerable<File> GetFiles()
            {
                //get list of children, filter through only files, and select them as new SimpleFiles
                return node.GetChildren()
                          .Where(c => c.IsFile == true)
                          .Select<VirtualNode, File>(c => new SimpleFile(c))
                          .ToList();
            }

            public Directory CreateDirectory(string name)
            {
                
                return new SimpleDirectory(node.CreateDirectoryNode(name));
            }

            public File CreateFile(string name)
            {
                return new SimpleFile(node.CreateFileNode(name));
            }
        }

        //
        // File
        //

        private class SimpleFile : SimpleEntry, File
        {
            public SimpleFile(VirtualNode node) : base(node)
            {
            }

            public int Length => node.FileLength;

            public FileStream Open()
            {
                return new SimpleStream(node);
            }

        }

        //
        // FileStream
        //

        private class SimpleStream : FileStream
        {
            private VirtualNode node;

            public SimpleStream(VirtualNode node)
            {
                this.node = node;
            }

            public void Close()
            {
                node = null;
            }

            public byte[] Read(int index, int length)
            {
                if (node == null)
                {
                    throw new Exception("Stream closed for further read!");
                }

                return node.Read(index,length);
            }

            public void Write(int index, byte[] data)
            {
                if (node == null)
                {
                    throw new Exception("Stream closed for further read!");
                }

                node.Write(index,data);
            }
        }

        #endregion
    }
}
