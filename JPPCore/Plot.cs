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
        public Dictionary<string, WallSegment> WallSegments;

        public string PlotName { get; set; } 
        
        public Plot()
        {
            WallSegments = new Dictionary<string, WallSegment>();
        }       

        public double FormationLevel { get; set; }

        public void Update()
        {
            foreach(WallSegment ws in WallSegments.Values)
            {
                ws.Update();
            }
        }
    }
}
