using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;
using Autodesk.Civil.DatabaseServices;

[assembly: CommandClass(typeof(JPP.Civils.PolylineUtilities))]

namespace JPP.Civils
{
    class PolylineUtilities
    {
        [CommandMethod("LevelPolyline")]
        public static void Level()
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
                    //Set max or min?
                    PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
                    pKeyOpts.Message = "\nSet to minimum or maximum level of line?";
                    pKeyOpts.Keywords.Add("Highest");
                    pKeyOpts.Keywords.Add("Lowest");
                    //pKeyOpts.Keywords.Default = "Minimum";
                    pKeyOpts.AllowNone = true;
                    PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);

                    foreach (SelectedObject so in psr.Value)
                    {
                        DBObject obj = tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if (obj is Polyline3d)
                        {
                            double? min = null;
                            double? max = null;
                            double targetLevel;

                            Polyline3d pl3d = obj as Polyline3d;
                            foreach (ObjectId id in pl3d)
                            {
                                PolylineVertex3d plv3d = tr.GetObject(id, OpenMode.ForRead) as PolylineVertex3d;
                                Point3d p3d = plv3d.Position;
                                if(min == null)
                                {
                                    min = p3d.Z;
                                }
                                if (max == null)
                                {
                                    max = p3d.Z;
                                }

                                if(p3d.Z < min)
                                {
                                    min = p3d.Z;
                                }
                                if(p3d.Z > max)
                                {
                                    max = p3d.Z;
                                }
                            }                            
                            switch (pKeyRes.StringResult)
                            {
                                case "Lowest":
                                    targetLevel = (double) min;
                                    break;

                                case "Highest":
                                    targetLevel = (double) max;
                                    break;

                                default:
                                    targetLevel = 0;
                                    break;
                            }

                            foreach (ObjectId id in pl3d)
                            {
                                PolylineVertex3d plv3d = tr.GetObject(id, OpenMode.ForWrite) as PolylineVertex3d;
                                Point3d p3d = plv3d.Position;
                                plv3d.Position = new Point3d(p3d.X, p3d.Y, targetLevel);
                            }
                        }
                        else
                        {
                            acDoc.Editor.WriteMessage("Object is not a polyline\n");
                        }
                    }

                    tr.Commit();
                }
            }
        }

        [CommandMethod("PlineToFFL")]
        public void PlineToFFL()
        {
            JPPCommandsInitialisation.JPPCommandsInitialise();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);
            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    //Get all model space drawing objects
                    TypedValue[] tv = new TypedValue[1];
                    tv.SetValue(new TypedValue(67, 0), 0);
                    SelectionFilter sf = new SelectionFilter(tv);
                    PromptSelectionResult allObjects = acDoc.Editor.SelectAll(sf);

                    foreach (SelectedObject target in psr.Value)
                    {
                        DBObject targetobj = tr.GetObject(target.ObjectId, OpenMode.ForRead);                        
                        if (targetobj is BlockReference)
                        {
                            BlockReference targetReference = targetobj as BlockReference;
                            foreach (SelectedObject candidate in allObjects.Value)
                            {
                                DBObject obj = tr.GetObject(candidate.ObjectId, OpenMode.ForRead);
                                if (obj is Polyline3d)
                                {
                                    Polyline3d pline3d = obj as Polyline3d;
                                    Point3d p3d = pline3d.GetPointAtDist(0);
                                    if (targetReference.Position.X == p3d.X && targetReference.Position.Y == p3d.Y)
                                    {
                                        EditFFL.EditFFLValue(target.ObjectId, Math.Ceiling(p3d.Z*20)/20);
                                    }
                                }
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        [CommandMethod("PlineToPlots")]
        public void PlineToPlots()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.SingleOnly = true;
            pso.RejectObjectsOnLockedLayers = true;
            PromptSelectionResult psr = acDoc.Editor.GetSelection(pso);
            if (psr.Status == PromptStatus.OK)
            {
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    CivSurface oSurface = null;

                    //Get the target surface
                    ObjectIdCollection SurfaceIds = CivilApplication.ActiveDocument.GetSurfaceIds();
                    foreach (ObjectId surfaceId in SurfaceIds)
                    {
                        CivSurface temp = surfaceId.GetObject(OpenMode.ForRead) as CivSurface;
                        if (temp.Name == Civils.Constants.ProposedGroundName)
                        {
                            oSurface = temp;
                        }

                        int plotCount = 0;

                        foreach (SelectedObject so in psr.Value)
                        {
                            DBObject obj = acTrans.GetObject(so.ObjectId, OpenMode.ForWrite);

                            if (obj is Curve)
                            {
                                plotCount++;

                                //Polyline acPline = obj as Polyline;

                                //Need to add the temp line to create feature line from it
                                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                                ObjectId perimId = FeatureLine.Create("plot" + plotCount, obj.ObjectId);

                                FeatureLine perim = acTrans.GetObject(perimId, OpenMode.ForWrite) as FeatureLine;
                                perim.AssignElevationsFromSurface(oSurface.Id, false);

                                double FinishedFloorLevel = Math.Round(perim.MaxElevation * 1000) / 1000 + 0.15;

                                //Ad the FFL Label
                                // Create a multiline text object
                                using (MText acMText = new MText())
                                {
                                    Solid3d Solid = new Solid3d();
                                    DBObjectCollection coll = new DBObjectCollection();
                                    coll.Add(obj);
                                    Solid.Extrude(((Region)Region.CreateFromCurves(coll)[0]), 1, 0);
                                    Point3d centroid = new Point3d(Solid.MassProperties.Centroid.X, Solid.MassProperties.Centroid.Y, 0);
                                    Solid.Dispose();

                                    acMText.Location = centroid;
                                    acMText.Contents = FinishedFloorLevel.ToString("F3");
                                    //acMText.Rotation = Rotation;
                                    acMText.Height = 7;
                                    acMText.Attachment = AttachmentPoint.MiddleCenter;

                                    acBlkTblRec.AppendEntity(acMText);
                                    acTrans.AddNewlyCreatedDBObject(acMText, true);
                                }

                                //perim.Erase();
                                obj.Erase();
                            }
                            else
                            {
                                acDoc.Editor.WriteMessage("Object is not a polyline\n");
                            }

                        }
                        
                        
                    }

                    acTrans.Commit();
                }
            }
        }
    }
}
