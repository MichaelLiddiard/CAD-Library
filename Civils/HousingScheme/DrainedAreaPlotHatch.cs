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
    public class DrainedAreaPlotHatch
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
            
            //Convert to 2d
            for (int index = 0; index < hatchBoundaryPoints.Count; index++)
            {
                hatchBoundary.AddVertexAt(index, new Point2d(hatchBoundaryPoints[index].X, hatchBoundaryPoints[index].Y), 0, 0, 0);
            }
            hatchBoundary.Closed = true;

            hatchBoundary.Layer = Properties.Settings.Default.Plot_Drained_Area_Layer;
            hatchBoundary.ColorIndex = Properties.Settings.Default.Plot_Drained_Area_Color;
            
            // Add the hatch boundary to modelspace
            Outline = acBlkTblRec.AppendEntity(hatchBoundary);
            acTrans.AddNewlyCreatedDBObject(hatchBoundary, true);

            // Add the hatch boundry to an object Id collection 
            ObjectIdCollection acObjIdColl = new ObjectIdCollection();
            acObjIdColl.Add(hatchBoundary.Id);

            // Set the hatch properties
            using (Hatch drainedAreaHatch = new Hatch())
            {
                Hatch = acBlkTblRec.AppendEntity(drainedAreaHatch);
                acTrans.AddNewlyCreatedDBObject(drainedAreaHatch, true);

                // Set the hatch properties
                drainedAreaHatch.SetHatchPattern(HatchPatternType.PreDefined, Properties.Settings.Default.Plot_Drained_Area_Pattern);
                drainedAreaHatch.Layer = Properties.Settings.Default.Plot_Drained_Area_Layer;
                drainedAreaHatch.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, 80);
                //drainedAreaHatch.PatternScale = 0.1;
                //drainedAreaHatch.PatternSpace = 0.1;
                //exposedHatch.PatternAngle = Constants.Deg_45;
                drainedAreaHatch.Associative = true;
                drainedAreaHatch.Annotative = AnnotativeStates.False;
                drainedAreaHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                drainedAreaHatch.EvaluateHatch(true);
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
