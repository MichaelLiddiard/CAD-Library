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
using System.Xml.Serialization;

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

        public float Step { get; set;  }

        public Shrinkage SoilShrinkage { get; set; }

        public SiteFoundations()
        {
            Trees = new List<NHBCTree>();            
            RingsCollection = new PersistentObjectIdCollection();
            Step = 0.1f; //Default to a sensible value otherwise its infinite
        }

        //public ObjectIdCollection RingsCollection = new ObjectIdCollection();        
        public PersistentObjectIdCollection RingsCollection { get; set; }

        public void GenerateTreeRings()
        {
            int[] ringColors = new int[] { 10,200,20,180,40,160,60,140,80,120,100 };

            //Determine start depth
            switch (SoilShrinkage)
            {
                case Shrinkage.High:
                    StartDepth = 1;
                    break;

                case Shrinkage.Medium:
                    StartDepth = 0.9f;
                    break;

                case Shrinkage.Low:
                    StartDepth = 0.75f;
                    break;
            }
            int maxSteps = 0;

            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                //Delete existing rings
                foreach (ObjectId obj in RingsCollection.Collection)
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

                    for (int i = 1; i < createdRegions.Count; i++)
                    {
                        enclosed.BooleanOperation(BooleanOperationType.BoolUnite, createdRegions[i]);
                    }

                    //Protection for color overflow, loop around
                    if (ringIndex >= ringColors.Length)
                    {
                        int multiple = (int)Math.Floor((double)(ringIndex / ringColors.Length));
                        enclosed.ColorIndex = ringColors[ringIndex - multiple * ringColors.Length];
                    } else
                    {
                        enclosed.ColorIndex = ringColors[ringIndex];
                    }

                    RingsCollection.Add(acBlkTblRec.AppendEntity(enclosed));
                    acTrans.AddNewlyCreatedDBObject(enclosed, true);

                }

                acTrans.Commit();
            }
        }

        public void UpdateDrawingObjects()
        {
            foreach(NHBCTree t in Trees)
            {
                t.CreateActiveObject();
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
                newTree.Generate();

                //TODO: Add tree determination in here
                PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
                pKeyOpts.Message = "\nTree Type ";
                pKeyOpts.Keywords.Add("Deciduous");
                pKeyOpts.Keywords.Add("Coniferous");                
                pKeyOpts.AllowNone = false;

                PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
                if(pKeyRes.StringResult == "Deciduous")
                {
                    newTree.TreeType = TreeType.Deciduous;
                } else
                {
                    newTree.TreeType = TreeType.Coniferous;
                }

                pKeyOpts = new PromptKeywordOptions("");
                pKeyOpts.Message = "\nWater deamnd ";
                pKeyOpts.Keywords.Add("High");
                pKeyOpts.Keywords.Add("Medium");
                if (newTree.TreeType == TreeType.Deciduous)
                {
                    pKeyOpts.Keywords.Add("Low");
                }
                pKeyOpts.AllowNone = false;

                pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
                Dictionary<string, int> speciesList = NHBCTree.DeciduousHigh;
                switch (pKeyRes.StringResult)
                {
                    case "High":
                        newTree.WaterDemand = WaterDemand.High;
                        if (newTree.TreeType == TreeType.Deciduous)
                        {
                            speciesList = NHBCTree.DeciduousHigh;
                        } else
                        {
                            speciesList = NHBCTree.ConiferousHigh;
                        }
                        break;

                    case "Medium":
                        newTree.WaterDemand = WaterDemand.Medium;
                        if (newTree.TreeType == TreeType.Deciduous)
                        {
                            speciesList = NHBCTree.DeciduousMedium;
                        }
                        else
                        {
                            speciesList = NHBCTree.ConiferousMedium;
                        }
                        break;

                    case "Low":
                        newTree.WaterDemand = WaterDemand.Low;
                        if (newTree.TreeType == TreeType.Deciduous)
                        {
                            speciesList = NHBCTree.DeciduousLow;
                        }
                        else
                        {
                            throw new ArgumentException(); //Doesnt exist!!
                        }
                        break;
                }

                pKeyOpts = new PromptKeywordOptions("");
                pKeyOpts.Message = "\nSpecies ";
                foreach (string s in speciesList.Keys)
                {
                    pKeyOpts.Keywords.Add(s);
                }
                                
                pKeyOpts.AllowNone = false;                
                pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
                newTree.Species = pKeyRes.StringResult;

                float maxHeight = (float)speciesList[newTree.Species];

                PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter tree height: ") { AllowSpaces = false, DefaultValue=maxHeight.ToString() };
                PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);

                float actualHeight = float.Parse(pStrResPlot.StringResult);                

                if (actualHeight < maxHeight / 2)
                {
                    newTree.Height = actualHeight;
                } else
                {
                    newTree.Height = maxHeight;
                }

                PromptPointOptions pPtOpts = new PromptPointOptions("\nClick to enter location: ");
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
