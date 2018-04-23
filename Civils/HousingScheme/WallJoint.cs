using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
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

        public long LevelLabelPtr;

        [XmlIgnore]
        public ObjectId LevelLabel
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(LevelLabelPtr), 0);
            }
            set
            {
                LevelLabelPtr = value.Handle.Value;
            }
        }

        public long LabelTextPtr;

        [XmlIgnore]
        public ObjectId LabelText
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(LabelTextPtr), 0);
            }
            set
            {
                LabelTextPtr = value.Handle.Value;
            }
        }

        [XmlIgnore]
        public double Level
        {
            get
            {
                if (this.IsAbsoluteLevel)
                {
                    return AbsoluteLevel;
                }
                else
                {
                    return parentFFL + RelativeOffset;
                }
            }
        }

        private double parentFFL;

        [XmlIgnore]
        private List<SegmentConnection> Segments;

        public JointType Type { get; set; }

        public double ExternalLevel { get; set; }
        public bool IsAbsoluteLevel { get; set; }
        public double RelativeOffset { get; set; }
        public double AbsoluteLevel { get; set; }

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
            }
            else
            {
                sc.Angle = Point.GetVectorTo(ws.StartPoint).GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis) * 180d / Math.PI;
            }

            Segments.Add(sc);
            Sort();
        }

        public WallSegment NextClockwise(WallSegment currentSegment)
        {
            double currentAngle = 0;
            foreach (SegmentConnection ws in Segments)
            {
                if (ws.Segment.Guid == currentSegment.Guid)
                {
                    currentAngle = ws.Angle;
                }
            }

            bool found = false;
            int i = 0;
            while (!found)
            {
                if (currentAngle <= Segments[i].Angle)
                {
                    found = true;
                }
                i++;
            }

            if (i > Segments.Count - 1)
            {
                //TODO: Make sure this works
                i = i - Segments.Count;
            }

            return Segments[i].Segment;
        }

        public void Generate(double Rotation)
        {
            if (Type == JointType.ExternalCorner || Type == JointType.AccessPoint)
            {
                Database acCurDb = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
                Transaction trans = acCurDb.TransactionManager.TopTransaction;

                this.LevelLabel = Core.Utilities.InsertBlock(Point, Rotation, "ProposedLevel");
                BlockReference acBlkTblRec = trans.GetObject(this.LevelLabel, OpenMode.ForRead) as BlockReference;
                foreach (ObjectId attId in acBlkTblRec.AttributeCollection)
                {
                    AttributeReference attDef = trans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;

                    if (attDef.Tag == "LEVEL")
                    {
                        this.LabelText = attDef.ObjectId;
                        //Set to level offset otherwise event handler overrides
                        attDef.TextString = ExternalLevel.ToString("F3");
                    }
                }
            }
        }

        public void Update(double FFL)
        {
            //Not all joints have labels, confirm
            if (LabelTextPtr != 0)
            {
                Database acCurDb = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
                Transaction trans = acCurDb.TransactionManager.TopTransaction;

                AttributeReference attDef = trans.GetObject(LabelText, OpenMode.ForWrite) as AttributeReference;

                //Set to level offset otherwise event handler overrides
                parentFFL = FFL;
                attDef.TextString = Level.ToString("F3");
            }
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

        public int Courses()
        {
            if (Level - ExternalLevel <= 0.15 && Level - ExternalLevel >= 0)
            {
                return 0;
            }
            else
            {
                double courses = (Level - ExternalLevel - 0.15) / 75;
                if(courses > 0)
                {
                    return (int) Math.Ceiling(courses);
                } else
                {
                    return (int)Math.Floor(courses);
                }
            }
        }        
    }

    public enum JointType
    {
        Internal,
        ExternalCorner,
        ExternalIntermediate,
        AccessPoint
    }
}
