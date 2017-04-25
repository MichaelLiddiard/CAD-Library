using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                                    foreach (ObjectId id in pline3d)
                                    {
                                        Point3d p3d = pline3d.GetPointAtDist(0);
                                        if (targetReference.Position.X == p3d.X && targetReference.Position.Y == p3d.Y)
                                        {
                                            EditFFL.EditFFLValue(target.ObjectId, p3d.Z);
                                        }
                                }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
