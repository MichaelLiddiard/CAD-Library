using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    class Library
    {
        /// <summary>
        /// Root directory to begin the library parse from
        /// </summary>
        public string LibraryRoot;

        /// <summary>
        /// String used to identify library blocks in drawing files
        /// </summary>
        public string Prefix;

        public LibraryNode RootNode;

        public Library(string Prefix)
        {
            this.Prefix = Prefix;
        }

        /// <summary>
        /// Parse the folder system and generate a tree of objects
        /// </summary>
        public void Regen()
        {
            RootNode = new LibraryNode();
            ParseLevel(LibraryRoot, RootNode);
        }

        private void ParseLevel(string path, LibraryNode current)
        {
            //Find all subdirectories, add as level on tree, then recurse
            var directories = Directory.EnumerateDirectories(path);
            foreach(string subdirectory in directories)
            {
                LibraryNode ln = new LibraryNode();
                ln.Parent = current;
                current.ChildNodes.Add(ln);
                ParseLevel(subdirectory, ln);
            }

            //Find all dwg files
            var files = Directory.EnumerateFiles(path, "*.dwg");
            foreach(string container in files)
            {
                ParseDWG(container, current);
            }
        }

        /// <summary>
        /// Opens the specified dwg file and parses for blocks with the correct prefix
        /// </summary>
        /// <param name="path">Path to dwg file</param>
        /// <param name="current">LibraryNode to add found items to</param>
        private void ParseDWG(string path, LibraryNode current)
        {
            //Iterate through all the blocks
            Database database = new Database(false, true);
            database.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, false, null);
            database.CloseInput(true);

            DrawingLibraryNode drawing = new DrawingLibraryNode();
            drawing.Parent = current;
            current.ChildNodes.Add(drawing);

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blkTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in blkTable)
                {
                    BlockTableRecord btRecord = (BlockTableRecord)transaction.GetObject(id, OpenMode.ForRead);
                    if (!btRecord.IsLayout)
                    {
                        //Access to the block (not model/paper space)
                        if(btRecord.Name.StartsWith(Prefix))
                        {
                            //BLock matching prefix found, add to tree                            
                            LibraryLeaf leaf = new LibraryLeaf();
                            leaf.Name = btRecord.Name.Remove(0, Prefix.Length); //Set a friendly name for the block
                            leaf.Parent = drawing;
                            drawing.ChildNodes.Add(leaf);
                        }
                    }
                }

                transaction.Commit();
            }

            //TODO: Do we need to stop cacheing the database?

        }
    }    
}
