using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.CivilStructures
{
    public class NHBCTree : CircleObject
    {
        public float Height;

        public string Species;

        public WaterDemand WaterDemand;

        public TreeType TreeType;

        public Shrinkage Shrinkage;

        public NHBCTree() : base()
        {
            
        }

        public override void Generate()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            // Open the Block table for read
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Open the Block table record Model space for write
            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            //Draw Trunk
            Circle trunk = new Circle();
            trunk.Center = new Point3d(0, 0, 0);
            trunk.Radius = 0.25;
            // Add the new object to the block table record and the transaction
            this.BaseObject = acBlkTblRec.AppendEntity(trunk);
            acTrans.AddNewlyCreatedDBObject(trunk, true);
        }
        
        public DBObjectCollection DrawRings(Shrinkage shrinkage, float StartDepth, float Step)
        {
            DBObjectCollection collection = new DBObjectCollection();

            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            // Open the Block table for read
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Open the Block table record Model space for write
            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            bool next = true;
            float currentDepth = StartDepth;
            Shrinkage = shrinkage;

            while (next)
            {
                float radius = GetRingRadius(currentDepth);

                if (radius > 0)
                {
                    Circle acCirc = new Circle();
                    acCirc.Center = new Point3d(Location.X, Location.Y, 0);
                    acCirc.Radius = radius;
                                        
                    collection.Add(acCirc);                   

                } else
                {
                    next = false;
                }

                currentDepth = currentDepth + Step;
            }

            return collection;
        }

        private float M()
        {
            switch (TreeType)
            {
                case TreeType.Coniferous:
                    switch (Shrinkage)
                    {
                        case Shrinkage.High:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return -0.25f;

                                case WaterDemand.Medium:
                                    return -0.25f;

                                case WaterDemand.Low:
                                    throw new NotImplementedException();
                            }
                            break;

                        case Shrinkage.Medium:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;

                        case Shrinkage.Low:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;
                    }
                    break;

                case TreeType.Deciduous:
                    switch (Shrinkage)
                    {
                        case Shrinkage.High:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return -0.5f;

                                case WaterDemand.Medium:
                                    return -0.542f;

                                case WaterDemand.Low:
                                    return -0.625f;
                            }
                            break;

                        case Shrinkage.Medium:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;

                        case Shrinkage.Low:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;
                    }
                    break;
            }

            return 0f;
        }

        private float C()
        {
            switch (TreeType)
            {
                case TreeType.Coniferous:
                    switch (Shrinkage)
                    {
                        case Shrinkage.High:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0.85f;

                                case WaterDemand.Medium:
                                    return 0.6f;

                                case WaterDemand.Low:
                                    throw new NotImplementedException();
                            }
                            break;

                        case Shrinkage.Medium:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;

                        case Shrinkage.Low:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;
                    }
                    break;

                case TreeType.Deciduous:
                    switch (Shrinkage)
                    {
                        case Shrinkage.High:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 1.75f;

                                case WaterDemand.Medium:
                                    return 1.29f;

                                case WaterDemand.Low:
                                    return 1.125f;
                            }
                            break;

                        case Shrinkage.Medium:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;

                        case Shrinkage.Low:
                            switch (WaterDemand)
                            {
                                case WaterDemand.High:
                                    return 0f;

                                case WaterDemand.Medium:
                                    return 0f;

                                case WaterDemand.Low:
                                    return 0f;
                            }
                            break;
                    }
                    break;
            }

            return 0f;
        }

        private float GetRingRadius(float foundationDepth)
        {
            float dh = M() * foundationDepth + C();

            return dh * Height;
        }

        public override void ActiveObject_Modified(object sender, EventArgs e)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            acDoc.SendStringToExecute("CS_GenerateRings ", false, false, false);
        }

        #region Tree Data
        public static Dictionary<string, int> DeciduousHigh = new Dictionary<string, int>()
        {
            { "EnglishElm",24 },
            { "WheatleyElm",22 },
            { "WhychElm",18 },
            { "Eucalyptus",18 },
            { "Hawthorn",10 },
            { "EnglishOak",20 },
            { "HolmOak",16 },
            { "RedOak",24 },
            { "TurkeyOak",24 },
            { "HybridBlackPoplar",28 },
            { "LombardyPoplar",25 },
            { "WhitePoplar",15 },
            { "CrackWillow",24 },
            { "WeepingWillow",16 },
            { "WhiteWillow",24 },
        };

        public static Dictionary<string, int> DeciduousMedium = new Dictionary<string, int>()
        {
            { "Acacia",18 },
            { "Alder",18 },
            { "Apple",10 },
            { "Ash",23 },
            { "BayLaurel",10 },
            { "Beech",20 },
            { "Blackthorn",8 },
            { "JapaneseCherry",9 },
            { "LaurelCherry",8 },
            { "OrchardCherry",12 },
            { "WildCherry",17 },
            { "HorseChestnut",20 },
            { "SweetChestnut",24 },
            { "Lime",22 },
            { "JapaneseMaple",8 },
            { "NorwayMaple",18 },
            { "MountainAsh",11 },
            { "Pear",12 },
            { "Plane",26 },
            { "Plum",10 },
            { "Sycamore",22 },
            { "TreeOfHeaven",20 },
            { "Walnut",18 },
            { "Whitebeam",12 },
        };

        public static Dictionary<string, int> DeciduousLow = new Dictionary<string, int>()
        {
            { "Birch",14 },
            { "Elder",10 },
            { "Fig",8 },
            { "Hazel",8 },
            { "Holly",12 },
            { "HoneyLocust",14 },
            { "Hornbeam",17 },
            { "Laburnum",12 },
            { "Magnolia",9 },
            { "Mulberry",9 },
            { "TulipTree",20 },

        };

        public static Dictionary<string, int> ConiferousHigh = new Dictionary<string, int>()
        {
            { "LawsonsCypress",18 },
            { "LeylandCypress",20 },
            { "MontereyCypress",20 },
        };

        public static Dictionary<string, int> ConiferousMedium = new Dictionary<string, int>()
        {
            { "Cedar",20 },
            { "DouglasFir",20 },
            { "Larch",20 },
            { "MonkeyPuzzle",18 },
            { "Pine",20 },
            { "Spruce",18 },
            { "Wellingtonia",30 },
            { "Yew",12 },
        };

        #endregion

    }

    public enum WaterDemand
    {
        Low,
        Medium,
        High
    }

    public enum TreeType
    {
        Deciduous,
        Coniferous
    }
}
