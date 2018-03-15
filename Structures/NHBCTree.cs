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
