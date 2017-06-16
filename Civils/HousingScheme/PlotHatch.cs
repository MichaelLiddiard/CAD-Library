using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPP.Civils
{
    public class PlotHatch
    {
        public long OutlinePtr;

        [XmlIgnore]
        public ObjectId Outline
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(OutlinePtr), 0);
            }
            set
            {
                OutlinePtr = value.Handle.Value;
            }
        }

        public long HatchPtr;

        [XmlIgnore]
        public ObjectId Hatch
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(HatchPtr), 0);
            }
            set
            {
                HatchPtr = value.Handle.Value;
            }
        }

        public PlotHatch()
        {

        }

        public void Generate(Point3dCollection hatchBoundaryPoints)
        {

            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            // Open the Block table for read
            BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Open the Block table record Model space for write
            BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Polyline hatchBoundary = new Polyline();

            // need to transform the outline by the block reference transform
            //BlockReference fflBlock = acTrans.GetObject(fflBlockId, OpenMode.ForRead) as BlockReference;

            for (int index = 0; index < hatchBoundaryPoints.Count; index++)
            {
                hatchBoundary.AddVertexAt(index, new Point2d(hatchBoundaryPoints[index].X, hatchBoundaryPoints[index].Y), 0, 0, 0);
            }
            hatchBoundary.Closed = true;
            /*if (isExposed)
                hatchBoundary.Layer = StyleNames.JPP_App_Exposed_Brick_Layer;
            else
                hatchBoundary.Layer = StyleNames.JPP_App_Tanking_Layer;*/
            hatchBoundary.Layer = StyleNames.JPP_App_Tanking_Layer;

            // Add the hatch boundary to modelspace

            Outline = acBlkTblRec.AppendEntity(hatchBoundary);
            acTrans.AddNewlyCreatedDBObject(hatchBoundary, true);

            // Add the hatch boundry to an object Id collection 
            ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            acObjIdColl.Add(hatchBoundary.Id);

            // Set the hatch properties
            using (Hatch exposedHatch = new Hatch())
            {
                Hatch = acBlkTblRec.AppendEntity(exposedHatch);
                acTrans.AddNewlyCreatedDBObject(exposedHatch, true);

                // Set the hatch properties
                exposedHatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                /*if (isExposed)
                {
                    exposedHatch.Layer = StyleNames.JPP_App_Exposed_Brick_Layer;
                    exposedHatch.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, 80);
                }
                else
                {
                    exposedHatch.Layer = StyleNames.JPP_App_Tanking_Layer;
                    exposedHatch.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, 130);
                }*/
                exposedHatch.Layer = StyleNames.JPP_App_Exposed_Brick_Layer;
                exposedHatch.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, 80);
                exposedHatch.PatternScale = 0.1;
                exposedHatch.PatternSpace = 0.1;
                //exposedHatch.PatternAngle = Constants.Deg_45;
                exposedHatch.Associative = true;
                exposedHatch.Annotative = AnnotativeStates.False;
                exposedHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                exposedHatch.EvaluateHatch(true);
            }
        }

        public void Erase()
        {
            Transaction acTrans = Application.DocumentManager.MdiActiveDocument.TransactionManager.TopTransaction;

            Entity hatchE = acTrans.GetObject(Hatch, OpenMode.ForWrite) as Entity;
            hatchE.Erase();
            Entity outlineE = acTrans.GetObject(Outline, OpenMode.ForWrite) as Entity;
            outlineE.Erase();
        }
    }
}
