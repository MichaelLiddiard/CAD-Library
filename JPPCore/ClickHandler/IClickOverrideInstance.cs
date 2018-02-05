using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public interface IClickOverrideInstance
    {
        bool CanHandle(DBObject obj);

        string CommandName();
    }
}
