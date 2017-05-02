using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    [Serializable]
    public class Plot
    {
        public Dictionary<string, ObjectId> WallSegments;

        public string PlotName { get; set; }        
    }
}
