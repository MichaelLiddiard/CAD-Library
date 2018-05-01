using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace JPP.Core
{
    public class Branch
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public ObservableCollection<Branch> ChildBranches { get; set; }
        public ObservableCollection<Leaf> Children { get; set; }

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

            ChildBranches = new ObservableCollection<Branch>();
            Children = new ObservableCollection<Leaf>();
        }
    }
}
