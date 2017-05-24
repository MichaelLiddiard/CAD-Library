using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    [Serializable]
    public class Plot
    {        
        public ObservableCollection<WallSegment> WallSegments { get; set; }

        public string PlotName { get; set; } 
        
        public Plot()
        {
            WallSegments = new ObservableCollection<WallSegment>();
        }       

        public double FormationLevel { get; set; }

        public double FinishedFloorLevel { get; set; }

        public bool Locked { get; set; }

        public void Update()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())//acCurDb.TransactionManager.StartTransaction())
            {
                foreach (WallSegment ws in WallSegments)
                {
                    ws.Update();
                }

                List<Curve> foundations = new List<Curve>();
                foreach (WallSegment ws in WallSegments)
                {
                    foundations.Add(tr.GetObject(ws.NegativeFoundationId, OpenMode.ForWrite) as Curve);
                    foundations.Add(tr.GetObject(ws.PositiveFoundationId, OpenMode.ForWrite) as Curve);
                }

                TrimFoundation(foundations);

                tr.Commit();
            }
        }

        public static List<Curve> TrimFoundation(List<Curve> allLines)
        {
            List<Curve> output = new List<Curve>();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            DBObjectCollection remove = new DBObjectCollection();
            List<Curve> perimeters = new List<Curve>();

            using (Transaction tr = acCurDb.TransactionManager.TopTransaction)//acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Curve c in allLines)
                {
                    Point3dCollection points = new Point3dCollection();

                    foreach (Curve target in allLines)
                    {
                        Point3dCollection pointsAppend = new Point3dCollection();
                        c.IntersectWith(target, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d p3d in pointsAppend)
                        {
                            points.Add(p3d);
                        }
                    }

                    if (points.Count == 2)
                    {
                        List<double> splitPoints = new List<double>();
                        foreach (Point3d p3d in points)
                        {
                            //splitPoints.Add(c.GetParameterAtPoint(p3d));
                            splitPoints.Add(c.GetParameterAtPoint(p3d));
                        }
                        splitPoints.Sort();
                        DoubleCollection acadSplitPoints = new DoubleCollection(splitPoints.ToArray());
                        DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                        acBlkTblRec.AppendEntity(remnant[1] as Entity);
                        tr.AddNewlyCreatedDBObject(remnant[1] as Entity, true);
                        output.Add(remnant[1] as Curve);
                        //c.HandOverTo(remnant[1] as Entity, true, true);
                        c.SwapIdWith(remnant[1].ObjectId, true, true);
                        remove.Add(c);
                    }

                    if (points.Count == 1)
                    {
                        List<double> splitPoints = new List<double>();
                        foreach (Point3d p3d in points)
                        {
                            //splitPoints.Add(c.GetParameterAtPoint(p3d));
                            splitPoints.Add(c.GetParameterAtPoint(p3d));
                        }
                        splitPoints.Sort();
                        DoubleCollection acadSplitPoints = new DoubleCollection(splitPoints.ToArray());

                        foreach (double d in acadSplitPoints)
                        {
                            double percent = c.GetDistanceAtParameter(d) / c.GetDistanceAtParameter(c.EndParam);
                            if (percent < 0.5 && percent > 0)
                            {
                                DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                                acBlkTblRec.AppendEntity(remnant[1] as Entity);
                                tr.AddNewlyCreatedDBObject(remnant[1] as Entity, true);
                                perimeters.Add(remnant[1] as Curve);
                                output.Add(remnant[1] as Curve);
                                //c.HandOverTo(remnant[1] as Entity, true, true);
                                c.SwapIdWith(remnant[1].ObjectId, true, true);
                                remove.Add(c);
                            }
                            if (percent < 1 && percent >= 0.5)
                            {
                                DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                                acBlkTblRec.AppendEntity(remnant[0] as Entity);
                                tr.AddNewlyCreatedDBObject(remnant[0] as Entity, true);
                                perimeters.Add(remnant[0] as Curve);
                                output.Add(remnant[0] as Curve);
                                //c.HandOverTo(remnant[0] as Entity, true, true);
                                c.SwapIdWith(remnant[0].ObjectId, true, true);
                                remove.Add(c);
                            }
                        }
                    }
                    if (points.Count == 0)
                    {
                        perimeters.Add(c);
                    }
                }

                foreach (DBObject obj in remove)
                {
                    obj.Erase();
                    //obj.Dispose();
                }

                List<Curve> complete = output;
                complete.AddRange(perimeters);

                //Iterate over the perimeter lines
                foreach (Curve c in perimeters)
                {
                    Point3dCollection points = new Point3dCollection();

                    foreach (Curve target in perimeters)
                    {
                        Point3dCollection pointsAppend = new Point3dCollection();
                        c.IntersectWith(target, Intersect.ExtendBoth, points, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d p3d in pointsAppend)
                        {
                            points.Add(p3d);
                        }
                    }

                    Vector3d newEnd = new Vector3d();
                    Vector3d newStart = new Vector3d();
                    double endDelta = Double.PositiveInfinity;
                    double startDelta = Double.PositiveInfinity;


                    foreach (Point3d p3d in points)
                    {
                        Line start = new Line(c.StartPoint, p3d);
                        Line end = new Line(c.EndPoint, p3d);

                        //Can only affect either end or start based on closest
                        if (start.Delta.LengthSqrd < end.Delta.LengthSqrd)
                        {
                            if (start.Delta.LengthSqrd < startDelta)
                            {
                                startDelta = start.Delta.LengthSqrd;
                                newStart = start.Delta;
                            }
                        }
                        else
                        {
                            if (end.Delta.LengthSqrd < endDelta)
                            {
                                endDelta = end.Delta.LengthSqrd;
                                newEnd = end.Delta;
                            }
                        }
                    }

                    //Check to see if lines meet at same point
                    foreach (Curve target in complete)
                    {
                        //Avoid self
                        if (target.ObjectId != c.ObjectId)
                        {

                            if (target.EndPoint == c.EndPoint || target.StartPoint == c.EndPoint)
                            {
                                newEnd = new Vector3d();
                            }
                            if (target.EndPoint == c.StartPoint || target.StartPoint == c.StartPoint)
                            {
                                newStart = new Vector3d();
                            }
                        }
                        else
                        {
                            int i = 0;
                        }
                    }

                    c.StartPoint = c.StartPoint + newStart;
                    c.EndPoint = c.EndPoint + newEnd;

                    output.Add(c);
                }

                //tr.Commit();
            }

            return output;
        }

        public void Generate()
        {
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            // Open the Block table for read
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            ObjectId blkRecId = ObjectId.Null;

            if (!acBlkTbl.Has("FormationTag"))
            {
                Utilities.LoadBlocks();
            }

            foreach (WallSegment ws in WallSegments)
            {
                ws.Generate();
            }
        }

        public void Rebuild()
        {
            foreach(WallSegment ws in WallSegments)
            {
                ws.Parent = this;
            }
        }
    }
}
