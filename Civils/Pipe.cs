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

[assembly: CommandClass(typeof(JPP.Civils.Pipe))]

namespace JPP.Civils
{
    public class Pipe
    {
        [CommandMethod("LayPipe")]
        public static void Lay()
        {
            bool promptGradient = false;
            int gradient = 0;

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nPlease enter mode";
            pKeyOpts.Keywords.Add("Gradient");
            pKeyOpts.Keywords.Add("Storm");
            pKeyOpts.Keywords.Add("Foul");
            pKeyOpts.Keywords.Default = "Storm";
            pKeyOpts.AllowNone = true;
            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            switch (pKeyRes.StringResult)
            {
                case "Gradient":
                    promptGradient = true;
                    break;

                case "Storm":
                    gradient = 100;
                    break;

                case "Foul":
                    gradient = 80;
                    break;
            }

            PromptStringOptions pStrOpts;
            PromptResult pStrRes;
            bool run = true;

            while (run)
            {
                if (promptGradient)
                {
                    //Prompt for the gradient
                    pStrOpts = new PromptStringOptions("\nEnter gradient: ");
                    pStrRes = acDoc.Editor.GetString(pStrOpts);
                    gradient = Int32.Parse(pStrRes.StringResult);
                }

                PromptPointResult pPtRes;
                PromptPointOptions pPtOpts = new PromptPointOptions("");

                // Prompt for the start point
                pPtOpts.Message = "\nEnter the start point of the line: ";
                pPtRes = acDoc.Editor.GetPoint(pPtOpts);
                if (pPtRes.Status == PromptStatus.Cancel)
                {
                    run = false;
                    break;
                }
                Point3d ptStart = pPtRes.Value;

                //Prompt for the starting IL
                pStrOpts = new PromptStringOptions("\nEnter starting IL: ");
                pStrOpts.AllowSpaces = true;
                pStrOpts.DefaultValue = ptStart.Z.ToString();
                pStrRes = acDoc.Editor.GetString(pStrOpts);
                if (pStrRes.Status == PromptStatus.Cancel)
                {
                    run = false;
                    break;
                }
                double invert = double.Parse(pStrRes.StringResult);

                // Prompt for the end point
                pPtOpts.Message = "\nEnter the end point of the line: ";
                pPtRes = acDoc.Editor.GetPoint(pPtOpts);
                if (pPtRes.Status == PromptStatus.Cancel)
                {
                    run = false;
                    break;
                }
                Point3d ptEnd = pPtRes.Value;
                
                //Adjust start coordinate IL
                ptStart = new Point3d(ptStart.X, ptStart.Y, invert); //Set to plane for accurate distance measure

                //Adjust end coordinate to gradient            
                Point3d temp = new Point3d(ptEnd.X, ptEnd.Y, ptStart.Z); //Set to plane for accurate distance measure
                double distance = Math.Abs(temp.DistanceTo(ptStart));
                double fall = distance / gradient;
                fall = Math.Round(fall, 3);
                ptEnd = new Point3d(temp.X, temp.Y, temp.Z - fall);

                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    // Open the Block table record for read
                    BlockTable acBlkTbl;
                    acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record M odel space for write
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Create a 3D polyline
                    using (Polyline3d acPoly3d = new Polyline3d())
                    {
                        // Add the new object to the block table record and the transaction
                        acBlkTblRec.AppendEntity(acPoly3d);
                        acTrans.AddNewlyCreatedDBObject(acPoly3d, true);

                        // Before adding vertexes, the polyline must be in the drawing
                        Point3dCollection acPts3dPoly = new Point3dCollection();
                        acPts3dPoly.Add(ptStart);
                        acPts3dPoly.Add(ptEnd);

                        foreach (Point3d acPt3d in acPts3dPoly)
                        {
                            using (PolylineVertex3d acPolVer3d = new PolylineVertex3d(acPt3d))
                            {
                                acPoly3d.AppendVertex(acPolVer3d);
                                acTrans.AddNewlyCreatedDBObject(acPolVer3d, true);
                            }
                        }

                        Annotate(acPoly3d);
                    }                  

                    // Save the new object to the database
                    acTrans.Commit();
                }
            }
        }

        [CommandMethod("AnnotatePipe")]
        public static void AnnotatePipe()
        {

        }

        public static void Annotate(Polyline3d acPoly3d)
        {
            Point3d labelPoint3d = acPoly3d.GetPointAtDist(acPoly3d.Length / 2);
            Point2d labelPoint = new Point2d(labelPoint3d.X, labelPoint3d.Y);
        }       
    }
}
