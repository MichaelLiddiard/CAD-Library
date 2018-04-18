using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public class Library<T>
    {
        string root;
        public List<Branch> Tree { get; set; }

        public Library(string basePath)
        {
            root = basePath;
            Update();
        }

        public void Update()
        {
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            Tree = Recurse(root);
        }

        private List<Branch> Recurse(string directory)
        {
            List<Branch> result = new List<Branch>();

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
    }        
}
