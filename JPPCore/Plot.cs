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
        public Dictionary<string, IntPtr> WallSegments;

        public string PlotName { get; set; } 
        
        public Plot()
        {
            WallSegments = new Dictionary<string, IntPtr>();
        }       

        public void AddWall(string ID, ObjectId obj)
        {
            WallSegments.Add(ID, obj.OldIdPtr);
        }

        public ObjectId GetWall(string ID)
        {
            return new ObjectId(WallSegments[ID]);
        }

        public double FormationLevel { get; set; }
    }
}
