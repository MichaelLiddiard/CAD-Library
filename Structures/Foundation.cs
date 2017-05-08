using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(JPP.Structures.Foundation))]

namespace JPP.Structures
{
    public class Foundation
    {
        [CommandMethod("CreateFoundation")]
        public static void CreateFoundation()
        {
            Main.CreateStructuralLayers();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);


            Plot newPlot = new Plot();
            List<Curve> allLines = new List<Curve>();

            PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter plot name: ");
            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);

            newPlot.PlotName = pStrRes.StringResult;

            pStrOpts = new PromptStringOptions("\nEnter FFL level: ");
            pStrOpts.AllowSpaces = true;
            pStrRes = acDoc.Editor.GetString(pStrOpts);

            newPlot.FormationLevel = double.Parse(pStrRes.StringResult);

            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if (obj is Curve)
                        {
                            Curve c = obj as Curve;
                            c.UpgradeOpen();
                            allLines.Add(c);
                        }
                    }

                    var centreLines = SplitFoundation(allLines);
                    TrimFoundation(GenerateFoundation(centreLines));
                    //TagFoundations(centreLines, newPlot);

                    foreach (Entity e in centreLines)
                    {
                        newPlot.WallSegments.Add(new WallSegment(newPlot.WallSegments.Count.ToString(), newPlot, e.ObjectId));
                    }

                    newPlot.Generate();

                    acDoc.SendStringToExecute("ATTSYNC N FormationTag\n", false, false, false);

                    tr.Commit();
                }
            }           

            DocumentStore.Current.Plots.Add(newPlot);
        }

        [CommandMethod("UpdateFoundation")]
        public static void UpdateFoundation()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);
            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        ObjectId extId = obj.ExtensionDictionary;

                        if (extId != ObjectId.Null)
                        {
                            //now we will have extId...
                            DBDictionary dbExt = (DBDictionary)tr.GetObject(extId, OpenMode.ForWrite);

                            if(dbExt.Contains("JPP_Plot"))
                            {
                                Xrecord xRec = tr.GetObject(dbExt.GetAt("JPP_Plot"), OpenMode.ForRead) as Xrecord;
                                string plot = xRec.Data.AsArray()[0].Value.ToString();
                                var target = from p in DocumentStore.Current.Plots where p.PlotName == plot select p; //DocumentStore.Current.Plots[plot];
                                foreach (Plot p in target)
                                {
                                    p.Update();
                                }
                            }                            
                        }
                    }

                    tr.Commit();
                }
            }
        }

        [CommandMethod("GenFound")]
        public static void GenerateFoundation()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);
            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    List<Curve> centreLines = new List<Curve>();
                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if (obj is Curve)
                        {
                            Curve c = obj as Curve;
                            centreLines.Add(c);
                        }
                    }
                    GenerateFoundation(centreLines);

                    tr.Commit();
                }
            }
        }

        public static List<Curve> GenerateFoundation(List<Curve> centreLines)
        {
            List<Curve> edgeLines = new List<Curve>();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;
                ObjectId current = acCurDb.Clayer;
                acCurDb.Clayer = acLyrTbl[Main.FoundationLayer];

                // Open the Block table for read
                BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Curve c in centreLines)
                {
                    DBObjectCollection offsets = c.GetOffsetCurves(0.3);
                    DBObjectCollection offsets2 = c.GetOffsetCurves(-0.3);
                    foreach (Entity e in offsets)
                    {
                        acBlkTblRec.AppendEntity(e);
                        tr.AddNewlyCreatedDBObject(e, true);
                        edgeLines.Add(e as Curve);
                        e.LayerId = acCurDb.Clayer;
                    }
                    foreach (Entity e in offsets2)
                    {
                        acBlkTblRec.AppendEntity(e);
                        tr.AddNewlyCreatedDBObject(e, true);
                        edgeLines.Add(e as Curve);
                        e.LayerId = acCurDb.Clayer;
                    }
                }

                acCurDb.Clayer = current;

                tr.Commit();
            }
            return edgeLines;
        }


        [CommandMethod("SplitFound")]
        public static void SplitFoundation()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);

            List<Curve> allLines = new List<Curve>();

            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if (obj is Curve)
                        {
                            Curve c = obj as Curve;
                            c.UpgradeOpen();
                            allLines.Add(c);
                        }
                    }

                    SplitFoundation(allLines);

                    tr.Commit();
                }
            }
        }

        public static List<Curve> SplitFoundation(List<Curve> allLines)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            List<Curve> centreLines = new List<Curve>();

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                DBObjectCollection remove = new DBObjectCollection();
                

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

                    if (points.Count > 0)
                    {
                        List<double> splitPoints = new List<double>();
                        foreach (Point3d p3d in points)
                        {
                            splitPoints.Add(c.GetParameterAtPoint(p3d));
                        }
                        splitPoints.Sort();
                        DoubleCollection acadSplitPoints = new DoubleCollection(splitPoints.ToArray());
                        DBObjectCollection split = c.GetSplitCurves(acadSplitPoints);
                        foreach (Entity e in split)
                        {
                            acBlkTblRec.AppendEntity(e);
                            tr.AddNewlyCreatedDBObject(e, true);
                            centreLines.Add(e as Curve);
                        }
                        remove.Add(c);
                    }
                }

                foreach (DBObject obj in remove)
                {
                    obj.Erase();
                }

                tr.Commit();

            }

            return centreLines;
        }

        [CommandMethod("TrimFound")]
        public static void TrimFoundation()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);
            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table for read
                    BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    List<Curve> allLines = new List<Curve>();

                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if (obj is Curve)
                        {
                            Curve c = obj as Curve;
                            c.UpgradeOpen();
                            allLines.Add(c);
                        }
                    }
                    TrimFoundation(allLines);

                    tr.Commit();
                }
            }
        }

        public static List<Curve> TrimFoundation(List<Curve> allLines)
        {
            List<Curve> output = new List<Curve>();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            DBObjectCollection remove = new DBObjectCollection();
            List<Curve> perimeters = new List<Curve>();            

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
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
                                remove.Add(c);
                            }
                            if (percent < 1 && percent >= 0.5)
                            {
                                DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                                acBlkTblRec.AppendEntity(remnant[0] as Entity);
                                tr.AddNewlyCreatedDBObject(remnant[0] as Entity, true);
                                perimeters.Add(remnant[0] as Curve);
                                output.Add(remnant[0] as Curve);
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
                        } else
                        {
                            int i = 0;
                        }
                    }

                    c.StartPoint = c.StartPoint + newStart;
                    c.EndPoint = c.EndPoint + newEnd;

                    output.Add(c);
                }               

                tr.Commit();
            }

            return output;
        }

        public static void TagFoundations(List<Curve> lines, Plot p)
        {
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            ObjectContextCollection occ = acCurDb.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;
                ObjectId current = acCurDb.Clayer;
                acCurDb.Clayer = acLyrTbl[Main.FoundationTextLayer];

                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                ObjectId blkRecId = ObjectId.Null;

                if (!acBlkTbl.Has("FormationTag"))
                {
                    Main.LoadBlocks();
                }

                foreach (Curve c in lines)
                {
                    Matrix3d curUCSMatrix = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
                    CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

                    //Get lable point
                    Point3d labelPoint3d = c.GetPointAtDist(c.GetDistanceAtParameter(c.EndParam) / 2);
                    Point3d labelPoint = new Point3d(labelPoint3d.X, labelPoint3d.Y, 0);

                    // Insert the block into the current space
                    using (BlockReference acBlkRef = new BlockReference(labelPoint, acBlkTbl["FormationTag"]))
                    {
                        //Calculate Line Angle
                        double y = c.EndPoint.Y - c.StartPoint.Y;
                        double x = c.EndPoint.X - c.StartPoint.X;
                        double angle = Math.Atan(Math.Abs(y) / Math.Abs(x));
                        if (angle >= Math.PI / 2)
                        {
                            acBlkRef.TransformBy(Matrix3d.Rotation(0, curUCS.Zaxis, labelPoint));
                        } else
                        {
                            acBlkRef.TransformBy(Matrix3d.Rotation(Math.PI/2, curUCS.Zaxis, labelPoint));
                        }
                        acBlkRef.AddContext(occ.GetContext("10:1"));

                        //Set value
                        AttributeCollection attCol = acBlkRef.AttributeCollection;
                        foreach (ObjectId attId in attCol)
                        {
                            AttributeReference att = acTrans.GetObject(attId, OpenMode.ForRead, false) as AttributeReference;
                            if (att.Tag == "LEVEL")
                            {
                                att.UpgradeOpen();
                                att.TextString = p.FormationLevel.ToString();
                            }
                        }

                        BlockTableRecord acCurSpaceBlkTblRec;
                        acCurSpaceBlkTblRec = acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                        acTrans.AddNewlyCreatedDBObject(acBlkRef, true);
                    }
                }

                acCurDb.Clayer = current;

                acTrans.Commit();
            }
        }

        public static void UpdateTags()
        {

        }
    }
}
