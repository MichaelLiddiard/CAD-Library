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

        public WallSegment North
        {
            get
            {
                return Segments.OrderBy(p => p.FromNorth).First().Segment;
            }
        }


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
                sc.Angle = Point.GetVectorTo(ws.EndPoint).GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis) * 180d / Math.PI;
            } else
            {
                sc.Angle = Point.GetVectorTo(ws.StartPoint).GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis) * 180d / Math.PI;
            }
            
            Segments.Add(sc);
            Sort();
        }

        public WallSegment NextClockwise(WallSegment currentSegment)
        {
            double currentAngle = 0;
            foreach(SegmentConnection ws in Segments)
            {
                if(ws.Segment.Guid == currentSegment.Guid)
                {
                    currentAngle = ws.Angle;
                }
            }

            bool found = false;
            int i = 0;
            while(!found)
            {
                if(currentAngle <= Segments[i].Angle)
                {
                    found = true;
                }
                i++;
            }

            if(i > Segments.Count - 1)
            {
                //TODO: Make sure this works
                i = i - Segments.Count;
            }

            return Segments[i].Segment;
        }
            
        
        struct SegmentConnection
        {
            public WallSegment Segment;
            public double Angle;          
            public double FromNorth
            {
                get
                {
                    if (Angle < 180)
                    {
                        return Angle;
                    }
                    else
                    {
                        return 360d - Angle;
                    }
                }
            }
        }
    }
}
