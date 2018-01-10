using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPP.Civils
{
    public class WallJoint
    {
        public Point3d Point;

        [XmlIgnore]
        private List<SegmentConnection> Segments;    
        
        public WallJoint()
        {
            Segments = new List<SegmentConnection>();            
        }

        public void Sort()
        {
            Segments = Segments.OrderBy(o => o.Angle).ToList();
        }

        public void AddWallSegment(WallSegment ws)
        {
            //TODO: Make sure actually connects to the point
            SegmentConnection sc = new SegmentConnection() { Segment = ws };
            if (ws.StartPoint == Point)
            {
                sc.Angle = Point.GetVectorTo(ws.EndPoint).GetAngleTo(Vector3d.YAxis);
            } else
            {
                sc.Angle = Point.GetVectorTo(ws.StartPoint).GetAngleTo(Vector3d.YAxis);
            }
            
            Segments.Add(sc);
            Sort();
        }                
        
        struct SegmentConnection
        {
            public WallSegment Segment;
            public double Angle;            
        }
    }
}
