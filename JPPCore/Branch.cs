using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public class Branch
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public List<Branch> ChildBranches { get; set; }
        public List<Leaf> Children { get; set; }

        public List<object> Combined
        {
            get
            {
                List<object> temp = new List<object>();
                temp.AddRange(ChildBranches);
                temp.AddRange(Children);
                return temp;
            }
        }

        public Branch(string path)
        {
            Path = path;
            Name = path.Split('\\').Last(); ;

            ChildBranches = new List<Branch>();
            Children = new List<Leaf>();
        }
    }
}
