using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.BoundaryRepresentation;
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

        ObjectIdCollection RingsCollection = new ObjectIdCollection();        

        public void GenerateTreeRings()
        {
            int[] ringColors = new int[] { 10,200,40,180,60,160,80,140,100,120 };

            StartDepth = 1;
            Step = 0.3f;
            int maxSteps = 0;

            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                //Delete existing rings
                foreach (ObjectId obj in RingsCollection)
                {
                    if (!obj.IsErased)
                    {
                        acTrans.GetObject(obj, OpenMode.ForWrite).Erase();
                    }
                }

                //Add the merged ring to the drawing
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                List<DBObjectCollection> rings = new List<DBObjectCollection>();

                //Generate the rings for each tree
                foreach (NHBCTree tree in Trees)
                {
                    DBObjectCollection collection = tree.DrawRings(SoilShrinkage, StartDepth, Step);
                    if (collection.Count > maxSteps)
                    {
                        maxSteps = collection.Count;
                    }

                    rings.Add(collection);
                }

                for (int ringIndex = 0; ringIndex < maxSteps; ringIndex++)
                {
                    //Determine overlaps
                    List<Curve> currentStep = new List<Curve>();
                    DBObjectCollection splitCurves = new DBObjectCollection();

                    //Build a collection of the outer rings only
                    foreach (DBObjectCollection col in rings)
                    {
                        //Check not stepping beyond
                        if (col.Count > ringIndex)
                        {
                            if (col[ringIndex] is Curve)
                            {
                                currentStep.Add(col[ringIndex] as Curve);
                            }
                        }
                    }
                    
                    List<Region> createdRegions = new List<Region>();

                    //Create regions
                    foreach (Curve c in currentStep)
                    {
                        DBObjectCollection temp = new DBObjectCollection();
                        temp.Add(c);
                        DBObjectCollection regions = Region.CreateFromCurves(temp);
                        foreach (Region r in regions)
                        {
                            createdRegions.Add(r);
                        }
                    }

                    Region enclosed = createdRegions[0];
                    enclosed.ColorIndex = ringColors[ringIndex];

                    for (int i = 1; i < createdRegions.Count; i++)
                    {
                        enclosed.BooleanOperation(BooleanOperationType.BoolUnite, createdRegions[i]);
                    }

                    enclosed.ColorIndex = ringColors[ringIndex];

                    RingsCollection.Add(acBlkTblRec.AppendEntity(enclosed));
                    acTrans.AddNewlyCreatedDBObject(enclosed, true);

                }

                acTrans.Commit();
            }
        }


        [CommandMethod("CS_NewTree")]
        public static void NewTree()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acDoc.TransactionManager.StartTransaction())
            {
                NHBCTree newTree = new NHBCTree();

                //TODO: Add tree determination in here
                PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter tree height: ") { AllowSpaces = false };
                PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);

                newTree.Height = float.Parse(pStrResPlot.StringResult);
                newTree.WaterDemand = WaterDemand.High;
                newTree.TreeType = TreeType.Deciduous;

                PromptPointOptions pPtOpts = new PromptPointOptions("\nEnter base point of the plot: ");
                PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);
                newTree.Location = new Autodesk.AutoCAD.Geometry.Point3d(pPtRes.Value.X, pPtRes.Value.Y, 0);

                SiteFoundations sf = acDoc.GetDocumentStore<CivilStructureDocumentStore>().SiteFoundations;
                
                sf.Trees.Add(newTree);
                sf.GenerateTreeRings();

                acTrans.Commit();
            }
        }

        [CommandMethod("CS_GenerateRings")]
        public static void CS_GenerateRings()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acDoc.TransactionManager.StartTransaction())
            {              
                SiteFoundations sf = acDoc.GetDocumentStore<CivilStructureDocumentStore>().SiteFoundations;                                
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
