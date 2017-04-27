using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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
                    // Open the Block table for read
                    BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if(obj is Curve)
                        {
                            Curve c = obj as Curve;
                            DBObjectCollection offsets = c.GetOffsetCurves(0.225);
                            DBObjectCollection offsets2 = c.GetOffsetCurves(-0.225);
                            foreach (Entity e in offsets)
                            {
                                acBlkTblRec.AppendEntity(e);
                                tr.AddNewlyCreatedDBObject(e, true);
                            }
                            foreach (Entity e in offsets2)
                            {
                                acBlkTblRec.AppendEntity(e);
                                tr.AddNewlyCreatedDBObject(e, true);
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        [CommandMethod("SplitFound")]
        public static void SplitFoundation()
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

                    DBObjectCollection remove = new DBObjectCollection();

                    foreach(Curve c in allLines)
                    {
                        Point3dCollection points = new Point3dCollection();

                        foreach (Curve target in allLines)
                        {
                            Point3dCollection pointsAppend = new Point3dCollection();
                            c.IntersectWith(target, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);      
                            foreach(Point3d p3d in pointsAppend)
                            {
                                points.Add(p3d);
                            }
                        }

                        if (points.Count > 0)
                        {
                            List<double> splitPoints = new List<double>();
                            foreach(Point3d p3d in points)
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
                            }
                            remove.Add(c);
                        }
                    }
                    
                    foreach(DBObject obj in remove)
                    {
                        obj.Erase();
                    }                     

                    tr.Commit();
                }
            }
        }
    }
}
