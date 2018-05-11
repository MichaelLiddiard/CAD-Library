using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.ObjectModel;
using System.IO;

namespace JPP.Core
{
    public class Library<T> where T : ILibraryItem, new()
    {
        string root;

        public ObservableCollection<Branch> Tree { get; set; }

        public Library(string basePath)
        {
            root = basePath;
            Update();
        }

        public void Update()
        {            
            if (Directory.Exists(root))
            {
                //Directory.CreateDirectory(root);
                Tree = Recurse(root);
            }            
        }

        private ObservableCollection<Branch> Recurse(string directory)
        {
            ObservableCollection<Branch> result = new ObservableCollection<Branch>();

            var dir = Directory.EnumerateDirectories(directory);
            foreach(string s in dir)
            {
                Branch b = new Branch(s);
                b.ChildBranches = Recurse(s);
                var leaves = Directory.EnumerateFiles(s, "*.dwg");
                {
                    foreach (string l in leaves)
                    {
                        Leaf newLeaf = new Leaf(l);
                        b.Children.Add(newLeaf);
                    }
                }
                result.Add(b);
            }            

            return result;
        }
        
        public void LoadLeafEntity(Leaf leaf)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            /*T t = new T();
            t.Transfer();//LoadFrom(leaf.Name, leaf.GetDatabase());
            return t;*/
            Database source = leaf.GetDatabase();

            T t = new T();
            t = (T)t.GetFrom(leaf.Name, source);
            t.Transfer(acCurDb, source);
        }

        public void SaveLeafEntity(string Name, T leafEntity, Branch parent)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (DocumentLock dl = acDoc.LockDocument())
            {

                Database target = new Database(true, false);
                leafEntity.Transfer(target, acCurDb);
                target.SaveAs(parent.Path + "\\" + Name + ".dwg", DwgVersion.Newest);
            }

            Recurse(root);
        }
    }        
}
