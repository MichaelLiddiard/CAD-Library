using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public interface ILibraryItem
    {
        void LoadFrom(string Name, Database from);
        void SaveTo(string Name, Database to);
    }
}
