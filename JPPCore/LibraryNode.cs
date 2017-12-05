using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    class LibraryNode
    {
        public LibraryNode Parent;

        public List<LibraryNode> ChildNodes;

        public string Name;
    }
}
