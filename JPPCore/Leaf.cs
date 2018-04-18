using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public class Leaf
    {
        public string Path { get; set; }
        public string Name { get; set; }

        public Leaf(string path)
        {
            Path = path;
            Name = path.Split('\\').Last().Replace(".dwg", "");
        }
    }
}
