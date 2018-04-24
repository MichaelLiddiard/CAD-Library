using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        
        public T GetLeafEntity(Leaf leaf)
        {
            T t = new T();
            t.LoadFrom(leaf.Name, leaf.GetDatabase());
            return t;
        }

        public void SaveLeafEntity(string Name, T leafEntity, Branch parent)
        {
            Database target = new Database(true, false);
            leafEntity.SaveTo(Name, target);
            target.SaveAs(parent.Path + Name + ".dwg", DwgVersion.Newest);
        }
    }        
}
