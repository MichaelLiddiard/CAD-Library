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

[assembly: CommandClass(typeof(JPP.CivilStructures.SiteFoundations))]

namespace JPP.CivilStructures
{
    public class SiteFoundations
    {
        /// <summary>
        /// Safe ground bearing pressure in kN/m2
        /// </summary>
        public int GroundBearingPressure { get; set; }

        /// <summary>
        /// Default width for all foundations
        /// </summary>
        public float DefaultWidth { get; set; }

        public List<NHBCTree> Trees;

        public float StartDepth;

        public float Step;

        public Shrinkage SoilShrinkage;

        public SiteFoundations()
        {
            Trees = new List<NHBCTree>();
            SoilShrinkage = Shrinkage.High;
        }

        private void GenerateTreeRings()
        {
            StartDepth = 1;
            Step = 0.3f;
            int maxSteps = 0;

            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            List<DBObjectCollection> rings = new List<DBObjectCollection>();            

            //Generate the rings for each tree
            foreach (NHBCTree tree in Trees)
            {
                DBObjectCollection collection = tree.DrawRings(SoilShrinkage, StartDepth, Step);
                if(collection.Count > maxSteps)
                {
                    maxSteps = collection.Count;
                }

                rings.Add(collection);
            }
            
            //Combine the rings, one stepping at a time
            for(int i = 0; i < maxSteps; i++)
            {
                List<Curve> currentStep = new List<Curve>();
                DBObjectCollection splitCurves = new DBObjectCollection();

                foreach (DBObjectCollection col in rings)
                {
                    //Check not stepping beyond
                    if(col.Count > i)
                    {
                        if(col[i] is Curve)
                        {
                            currentStep.Add(col[i] as Curve);
                        }                        
                    }
                }

                for(int currentCurveIndex = 0; currentCurveIndex < currentStep.Count; currentCurveIndex++)
                {
                    Point3dCollection intersections = new Point3dCollection();                    
                    for (int targetIndex = 0; targetIndex < currentStep.Count; targetIndex++)
                    {
                        //Make sure not testing against itself
                        if(currentCurveIndex != targetIndex)
                        {
                            Point3dCollection temp = new Point3dCollection();
                            currentStep[currentCurveIndex].IntersectWith(currentStep[targetIndex], Intersect.OnBothOperands, intersections, new IntPtr(0), new IntPtr(0));
                            foreach (Point3d p in temp)
                            {
                                intersections.Add(p);
                            }                            
                        }
                    }

                    if (intersections.Count > 0)
                    {
                        var result = currentStep[currentCurveIndex].GetSplitCurves(intersections);                        
                        foreach (Curve c in result)
                        {
                            splitCurves.Add(c);
                        }
                    } else
                    {
                        splitCurves.Add(currentStep[currentCurveIndex]);
                    }

                    intersections = new Point3dCollection();
                }

                //Now go over and merge

                //Add the merged ring to the drawing
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Curve c in splitCurves)
                {
                    acBlkTblRec.AppendEntity(c);
                    acTrans.AddNewlyCreatedDBObject(c, true);
                }
            }
        }


        [CommandMethod("CS_NewTree")]
        public static void NewPlot()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            NHBCTree newTree = new NHBCTree();

            //TODO: Add tree determination in here
            PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter tree height: ") { AllowSpaces = false };
            PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);

            newTree.Height = float.Parse(pStrResPlot.StringResult);
            newTree.WaterDemand = WaterDemand.High;
            newTree.TreeType = TreeType.Deciduous;

            PromptPointOptions pPtOpts = new PromptPointOptions("\nEnter base point of the plot: ");
            PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);
            newTree.Location = new Autodesk.AutoCAD.Geometry.Point2d(pPtRes.Value.X, pPtRes.Value.Y);

            SiteFoundations sf = acDoc.GetDocumentStore<CivilStructureDocumentStore>().SiteFoundations;

            using (Transaction acTrans = acDoc.TransactionManager.StartTransaction())
            {
                sf.Trees.Add(newTree);
                sf.GenerateTreeRings();

                acTrans.Commit();
            }
        }
    }

    public enum Shrinkage
    {
        Low,
        Medium,
        High
    }
}
