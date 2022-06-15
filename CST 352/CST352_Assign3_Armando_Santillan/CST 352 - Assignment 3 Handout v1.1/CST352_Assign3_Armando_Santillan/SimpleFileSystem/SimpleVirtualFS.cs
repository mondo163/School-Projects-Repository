// SimpleVirtualFS.cs
// Pete Myers
// Spring 2018-2020
//
// NOTE: Implement the methods and classes in this file
//
// Armando Santillan - 5/21/22
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFileSystem
{
    // NOTE:  Blocks are used for file data, directory contents are just stored in linked sectors (not blocks)

    public class VirtualFS
    {
        private const int DRIVE_INFO_SECTOR = 0;
        private const int ROOT_DIR_SECTOR = 1;
        private const int ROOT_DATA_SECTOR = 2;

        private Dictionary<string, VirtualDrive> drives;    // mountPoint --> drive
        private VirtualNode rootNode;

        public VirtualFS()
        {
            this.drives = new Dictionary<string, VirtualDrive>();
            this.rootNode = null;
        }

        public void Format(DiskDriver disk)
        {
            // wipe all sectors of disk and create minimum required DRIVE_INFO, DIR_NODE and DATA_SECTOR

            //make disk free sectors
            FREE_SECTOR free = new FREE_SECTOR(disk.BytesPerSector);
            byte[] rawBytes = free.RawBytes;
            for (int i = 0; i < disk.SectorCount; i++)
            {
                //make it free
                disk.WriteSector(i, rawBytes);
            }

            //create drive info
            DRIVE_INFO driveInfo = new DRIVE_INFO(disk.BytesPerSector, ROOT_DIR_SECTOR);
            disk.WriteSector(DRIVE_INFO_SECTOR, driveInfo.RawBytes);

            //create directory node
            DIR_NODE dir = new DIR_NODE(disk.BytesPerSector, ROOT_DATA_SECTOR, "/",0);
            disk.WriteSector(ROOT_DIR_SECTOR, dir.RawBytes);

            //create a data sector
            DATA_SECTOR data = new DATA_SECTOR(disk.BytesPerSector,0,null);
            disk.WriteSector(ROOT_DATA_SECTOR, data.RawBytes);


        }

        public void Mount(DiskDriver disk, string mountPoint)
        {
            // read drive info from disk, load root node and connect to mountPoint
            // for the first mounted drive, expect mountPoint to be named FSConstants.PATH_SEPARATOR as the root
            if (drives.Count == 0 && mountPoint != FSConstants.PATH_SEPARATOR.ToString())
            {
                throw new Exception("First mounted drive must be at '/' ");
            }
            
            try
            {
                //read drive info
                DRIVE_INFO driveInfo = DRIVE_INFO.CreateFromBytes(disk.ReadSector(DRIVE_INFO_SECTOR));

                // read dir node for root
                DIR_NODE rootDirNode = DIR_NODE.CreateFromBytes(disk.ReadSector(driveInfo.RootNodeAt));

                // read data sector for root
                DATA_SECTOR rootDataSector = DATA_SECTOR.CreateFromBytes(disk.ReadSector(rootDirNode.FirstDataAt));

                // instantiate a virtualdirve for this disk and add it to the drives dictionary
                VirtualDrive drive = new VirtualDrive(disk, DRIVE_INFO_SECTOR, driveInfo);
                drives.Add(mountPoint, drive);

                // instatiate a virtualNode for the root and set the root node
                rootNode = new VirtualNode(drive, driveInfo.RootNodeAt, rootDirNode, null);
            }
            catch(Exception ex)
            {
                throw new Exception("Unable to mount disk!", ex);
            }
        }

        public void Unmount(string mountPoint)
        {
            // look up the drive and remove it's mountPoint
            //if this containes the root of the file system, then clear the whole file system
            if (!drives.ContainsKey(mountPoint))
            {
                throw new Exception("Mount point not found, cannot mount!");
            }
            if (mountPoint == FSConstants.PATH_SEPARATOR.ToString())
            {
                drives.Clear(); // unmounting the root destroys everything
                rootNode = null;
            }
            else
                drives.Remove(mountPoint); //otherwise remove only the file system
        }

        public VirtualNode RootNode => rootNode;
    }

    public class VirtualDrive
    {
        private int bytesPerDataSector;
        private DiskDriver disk;
        private int driveInfoSector;
        private DRIVE_INFO sector;      // caching entire sector for now

        public VirtualDrive(DiskDriver disk, int driveInfoSector, DRIVE_INFO sector)
        {
            this.disk = disk;
            this.driveInfoSector = driveInfoSector;
            this.bytesPerDataSector = DATA_SECTOR.MaxDataLength(disk.BytesPerSector);
            this.sector = sector;
        }

        public int[] GetNextFreeSectors(int count)
        {
            // find count available free sectors on the disk and return their addresses
            int[] result = new int[count];
            int i = 0;

            //loop through all sectors on the disk
            for (int lba = 0; lba < disk.SectorCount && i < count; lba++)
            {
                //is the sector free
                if (SECTOR.GetTypeFromBytes(disk.ReadSector(lba)) == SECTOR.SectorType.FREE_SECTOR)
                {
                    result[i++] = lba;
                }
            }
            //if there are not enough free sectors, throw an exception
            if (i < count)
                throw new Exception("Not enough free sectors available!!");

            return result;
        }

        public DiskDriver Disk => disk;
        public int BytesPerDataSector => bytesPerDataSector;
    }

    public class VirtualNode
    {
        private VirtualDrive drive;
        private int nodeSector;
        private NODE sector;                                // caching entire sector for now
        private VirtualNode parent;
        private Dictionary<string, VirtualNode> children;   // child name --> child node
        private List<VirtualBlock> blocks;                  // cache of file blocks

        public VirtualNode(VirtualDrive drive, int nodeSector, NODE sector, VirtualNode parent)
        {
            this.drive = drive;
            this.nodeSector = nodeSector;
            this.sector = sector;
            this.parent = parent;
            this.children = null;                           // initially empty cache
            this.blocks = null;                             // initially empty cache
        }

        public VirtualDrive Drive => drive;
        public string Name => sector.Name;
        public VirtualNode Parent => parent;
        public bool IsDirectory { get { return sector.Type == SECTOR.SectorType.DIR_NODE; } }
        public bool IsFile { get { return sector.Type == SECTOR.SectorType.FILE_NODE; } }
        public int ChildCount => (sector as DIR_NODE).EntryCount;
        public int FileLength => (sector as FILE_NODE).FileSize;
        
        public void Rename(string name)
        {
            // rename this node, update parent as needed, save new name on disk
            // TODO: Extra credit - VirtualNode.Rename()
        }

        public void Move(VirtualNode destination)
        {
            // remove this node from it's current parent and attach it to it's new parent
            // update the directory information for both parents on disk
            // TODO: Extra Credit - VirtualNode.Move()
        }

        public void Delete()
        {
            // make sectors free!
            // wipe data for this node from the disk
            // wipe this node from parent directory from the disk
            // remove this node from it's parent node

            // TODO: Extra Credit - VirtualNode.Delete()
        }

        private void LoadChildren()
        {
            if (!IsDirectory)
                throw new Exception("Can't load blocks unless it's a Directory");

            //load up the cache of children for a directory
            //instantiate the children if the dictionary is null
            if (children == null)
            {
                children = new Dictionary<string, VirtualNode>();
                

                //read the list of children for this directory from disk and add them to the cache
                int dataSectorAddr = sector.NextSectorAt;
                DATA_SECTOR dataSector = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(dataSectorAddr));
                //datasector.databytes contains the list of children that could be a DIR_NODE or FILE_NODE
                //sector addresss are 4 bytes
                byte[] childList = dataSector.DataBytes;
                for (byte i = 0; i < ChildCount; i++)
                {
                    int addressAt = i * 4;
                    int childNodeAddr = BitConverter.ToInt32(childList, addressAt);

                    //read the child's node sector and data sector
                    NODE childNode = NODE.CreateFromBytes(drive.Disk.ReadSector(childNodeAddr));

                    //instantiate a VirtualNode for the child
                    VirtualNode child = new VirtualNode(drive, childNodeAddr, childNode, this);

                    //add the virtual node to the children cache
                    children.Add(child.Name, child);
                }
            }
        }

        private void CommitChildren()
        {
            if (!IsDirectory)
                throw new Exception("Can't commit blocks unless it's a Directory");
            //writing any changes in the cache back to disk
            if (children != null)
            {
                //turn the list into a byte[] are child node sector addressses
                int dataSectorAddr = sector.NextSectorAt;
                DATA_SECTOR dataSector = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(dataSectorAddr));
                byte [] childList = dataSector.DataBytes;
                int newChildCount = 0;
                foreach (VirtualNode child in children.Values)
                {
                    int childNodeaddr = child.nodeSector;
                    BitConverter.GetBytes(childNodeaddr).CopyTo(childList, newChildCount * 4);
                    newChildCount++; //
                }

                //write the child list to this directory's DATA_SECTOR 
                dataSector.DataBytes = childList;
                drive.Disk.WriteSector(dataSectorAddr, dataSector.RawBytes);

                //update this director's child count, if needed
                if (newChildCount != ChildCount)
                {
                    (sector as DIR_NODE).EntryCount = newChildCount;
                    drive.Disk.WriteSector(nodeSector, sector.RawBytes);
                }
            }

        }
        private VirtualNode CreateChildNode<NodeType>(string name) 
        {
            //child file or directory node

            //load the cache of children for this directory or file
            LoadChildren();

            //create the new child 
            //find 2 free sectors
            int[] freesectors = drive.GetNextFreeSectors(2);
            int childNodeSectorAddr = freesectors[0];
            int childDataSectorAddr = freesectors[1];

            //allocate a DATA_SECTOR and file/dir node sector
            DATA_SECTOR childDataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, null);
            drive.Disk.WriteSector(childDataSectorAddr, childDataSector.RawBytes);

            NODE childNodeSector = (NODE)Activator.CreateInstance(typeof(NodeType),
                new object[] { drive.Disk.BytesPerSector, childDataSectorAddr, name, 0 });

            drive.Disk.WriteSector(childNodeSectorAddr, childNodeSector.RawBytes);

            //instantiate a directory virtual node
            VirtualNode childNode = new VirtualNode(drive, childNodeSectorAddr, childNodeSector, this);

            //add node to the cache
            children.Add(name, childNode);

            //write the cache to disk
            CommitChildren();

            return childNode;
        }
        public VirtualNode CreateDirectoryNode(string name)
        {
            return CreateChildNode<DIR_NODE>(name);
        }

        public VirtualNode CreateFileNode(string name)
        {
            return CreateChildNode<FILE_NODE>(name);
        }

        public IEnumerable<VirtualNode> GetChildren()
        {
            //Make sure our children are cached in memory
            LoadChildren();

            //return the list of children
            return children.Values;
        }

        public VirtualNode GetChild(string name)
        {
            //return the virtual node for the named child or null

            //make sure our children are cached in memory
            LoadChildren();

            //find and return the name child
            return children.ContainsKey(name)?children[name]:null;
        }

        private void LoadBlocks()
        {
            if (!IsFile)
                throw new Exception("Can't load blocks unless it's a file");

            //read each data sector for this file from disk and create the in memory cache
            if (blocks == null)
            {
                blocks = new List<VirtualBlock>();

                //read each sector of the file from disk
                int nextDataSectorAddr = sector.NextSectorAt;
                while (nextDataSectorAddr!= 0)
                {
                    //read the data sector
                    DATA_SECTOR dataSector = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(nextDataSectorAddr));

                    //create a virtual blcok and add it to the cache
                    VirtualBlock block = new VirtualBlock(drive, nextDataSectorAddr, dataSector);
                    blocks.Add(block);

                    //find the next data sector 
                    nextDataSectorAddr = dataSector.NextSectorAt;
                }
            }
        }

        private void CommitBlocks()
        {
            if (!IsFile)
                throw new Exception("Can't commit blocks unless it's a file");

            //write each sector to disk
            if (blocks != null)
            {
                foreach (VirtualBlock item in blocks)
                {
                    item.CommitBlock();
                }
            }
             
        }

        public byte[] Read(int index, int length)
        {
            //read data from the files blocks and return it

            //makde sure the blcok cache is up to date
            LoadBlocks();

            //read data from the blocks
            byte[] data = VirtualBlock.ReadBlockData(Drive, blocks, index, length);

            return data;
        }

        public void Write(int index, byte[] data)
        {
            //write data into the file's blocks and update sectors on disk to match

            //make sure the block cache is up to date
            LoadBlocks();

            //grow the file if necessary
            int finalFileLength = Math.Max(FileLength,index + data.Length);
            if (finalFileLength > FileLength)
            {  
                //extend teh block cache
                VirtualBlock.ExtendBlocks(drive, blocks, FileLength, finalFileLength);

                //update the file length in the FILE_NODE sector
                (sector as FILE_NODE).FileSize = finalFileLength;
                drive.Disk.WriteSector(nodeSector, sector.RawBytes);
            }
            //write data into the blocks,
            VirtualBlock.WriteBlockData(drive, blocks, index, data);

            //Make sure the sectos on disk match the blcoks in memory
            CommitBlocks();
        }
    }

    public class VirtualBlock
    {
        private VirtualDrive drive;
        private DATA_SECTOR sector;
        private int sectorAddress;
        private bool dirty;

        public VirtualBlock(VirtualDrive drive, int sectorAddress, DATA_SECTOR sector, bool dirty = false)
        {
            this.drive = drive;
            this.sector = sector;
            this.sectorAddress = sectorAddress;
            this.dirty = dirty;
        }

        public int SectorAddress => sectorAddress;
        public DATA_SECTOR Sector => sector;
        public bool Dirty => dirty;

        public byte[] Data
        {
            get { return (byte[])sector.DataBytes.Clone(); }
            set
            {
                sector.DataBytes = value;
                dirty = true;
            }
        }

        public void CommitBlock()
        {
            //write this blocks sector to disk, if necessary
            if (dirty)
            {
                //write the data sector to disk
                drive.Disk.WriteSector(sectorAddress,sector.RawBytes);

                //make it clean again
                dirty = false;
            }
        }

        public static byte[] ReadBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, int length)
        {
            //reading the data in the list of blocks, startin at startIndex for length bytes
            //create empty buffer to contain the bytes we read from disk.
            byte[] data = new byte[length];

            int blockSize = drive.BytesPerDataSector;

            //calculate the initial block, to start reading data
            int initialBlockIndex = startIndex / blockSize;

            //calculate index of the final block, where the last data is read
            int finalBlockIndex = (startIndex + length) / blockSize;

            //read first block and each full block
            int currentDataBytesIndex = 0;
            int initialByteIndexWithinBlock = startIndex % blockSize;
            for (int blockIndex = initialBlockIndex; blockIndex < finalBlockIndex; blockIndex++)
            {
                int bytesToReadfromBlock = blockSize - initialByteIndexWithinBlock;

                //copy the bytes from the block into the output data
                byte[] blockData = blocks[blockIndex].Data;
                CopyBytes(bytesToReadfromBlock, blockData, initialByteIndexWithinBlock, data, currentDataBytesIndex);

                //advance indexes
                currentDataBytesIndex += bytesToReadfromBlock;
                initialByteIndexWithinBlock = 0; //start at the beginning

            }

            //write final block
            int bytesToReadFromFinalBlock = (startIndex + length) % blockSize;
            //copy bytes from final blcok to output data
            byte[] finalBlockData = blocks[finalBlockIndex].Data;
            CopyBytes(bytesToReadFromFinalBlock, finalBlockData, initialByteIndexWithinBlock, data, currentDataBytesIndex);

            return data;
        }

        public static void WriteBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, byte[] data)
        {
            //overwriting the data in the list of blocks, startin at startIndex, with data byte[]
            int blockSize = drive.BytesPerDataSector;

            //calculate the initial block, to start writing data
            int initialBlockIndex = startIndex / blockSize;
            //calculate index of the final block, where the last data is written
            int finalBlockIndex =(startIndex + data.Length) / blockSize;

            //write first block and each full block
            int currentDataBytesIndex = 0;
            int initialByteIndexWithinBlock = startIndex %blockSize;
            for (int blockIndex = initialBlockIndex; blockIndex < finalBlockIndex; blockIndex++) 
            {
                int bytesToWriteInBlock = blockSize - initialByteIndexWithinBlock;

                //copy the bytes from the input data to the block
                byte[] blockData = blocks[blockIndex].Data;
                CopyBytes(bytesToWriteInBlock, data, currentDataBytesIndex, blockData, initialByteIndexWithinBlock);
                blocks[blockIndex].Data = blockData;

                //advance indexes
                currentDataBytesIndex += bytesToWriteInBlock;
                initialByteIndexWithinBlock = 0; //start at the beginning

            }
            //write final block
            int bytesToWriteInFinalBlock = (startIndex + data.Length) % blockSize;
            //copy bytes from input data to the final block
            byte[] finalBlockData = blocks[finalBlockIndex].Data;
            CopyBytes(bytesToWriteInFinalBlock, data, currentDataBytesIndex, finalBlockData, initialByteIndexWithinBlock);
            blocks[finalBlockIndex].Data = finalBlockData;

        }

        public static void ExtendBlocks(VirtualDrive drive, List<VirtualBlock> blocks, int initialFileLength, int finalFileLength)
        {
            //add more blocks/sector to the file as necessary

            //determine how many blocks/sectors to add, based on initial and final file lengths
            int blocksNeeded = BlocksNeeded(drive, finalFileLength);
            int addBlockCount = blocksNeeded - blocks.Count;

            if (addBlockCount > 0)
            {
                //find that many FREE_SECTORS
                int[] addBlockAddress = drive.GetNextFreeSectors(addBlockCount);

                //for each block to add...
                foreach(int addBlockaddr in addBlockAddress)
                {
                    //change FREE_SECTOR to a DATA_SECTOR
                    DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, null);
                    drive.Disk.WriteSector(addBlockaddr, dataSector.RawBytes);

                    //set the previous sector's "nesdDataAT" to point to this new sector
                    blocks.Last().Sector.NextSectorAt = addBlockaddr;
                    drive.Disk.WriteSector(blocks.Last().SectorAddress, blocks.Last().Sector.RawBytes);

                    //instantiate a new VirtualBlock for the new data sector
                    VirtualBlock addBlock = new VirtualBlock(drive, addBlockaddr, dataSector, false);

                    //add the new block to the end of the block list
                    blocks.Add(addBlock);
                }
            }
        }

        private static int BlocksNeeded(VirtualDrive drive, int numBytes)
        {
            return Math.Max(1, (int)Math.Ceiling((double)numBytes / drive.BytesPerDataSector));
        }

        private static void CopyBytes(int copyCount, byte[] from, int fromStart, byte[] to, int toStart)
        {
            for (int i = 0; i < copyCount; i++)
            {
                to[toStart + i] = from[fromStart + i];
            }
        }
    }
}
