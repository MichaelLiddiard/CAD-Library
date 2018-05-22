using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly:CommandClass(typeof(JPP.Civils.Drainage.Manhole))]

namespace JPP.Civils.Drainage
{
    class Manhole
    {
        public int Diameter { get; set; }
        public float InvertLevel { get; set; }
        public float CoverLevel { get; set; }

        public Point3d IntersectionPoint { get; set; }
        public List<PipeConnection> IncomingPipes { get; set; }

        public PipeConnection outgoingConnection { get; set; }

        //Helper properties
        public int LargestInternalPipeDiameter
        {
            get
            {
                int largest = 0;
                foreach (PipeConnection pipeConnection in IncomingPipes)
                {
                    if (pipeConnection.Diameter > largest)
                    {
                        largest = pipeConnection.Diameter;
                    }
                }

                if (outgoingConnection.Diameter > largest)
                {
                    largest = outgoingConnection.Diameter;
                }

                return largest;
            }
        }
        
        public Manhole()
        {
            IncomingPipes = new List<PipeConnection>();
        }
        
        public void GeneratePlan(Point3d location)
        {
            DrainageNetwork.Current.Standard.VerifyManhole(this);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                Vector3d offset = location.GetAsVector().Subtract(IntersectionPoint.GetAsVector());

                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                LayerTable acLayerTable = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;
                Core.Utilities.CreateLayer(tr, acLayerTable, Constants.JPP_D_PipeWalls, Constants.JPP_D_PipeWallColor);
                Core.Utilities.CreateLayer(tr, acLayerTable, Constants.JPP_D_PipeCentreline, Constants.JPP_D_PipeCentrelineColor);
                Core.Utilities.CreateLayer(tr, acLayerTable, Constants.JPP_D_ManholeWall, Constants.JPP_D_ManholeWallColor);

                foreach (PipeConnection pipeConnection in IncomingPipes)
                {
                    Line newLine = new Line(location, pipeConnection.Location.Add(offset));
                    newLine.Layer = Constants.JPP_D_PipeCentreline;

                    Line offsetPlus = newLine.GetOffsetCurves(pipeConnection.Diameter / 2)[0] as Line;
                    offsetPlus.Layer = Constants.JPP_D_PipeWalls;
                    Line offsetMinus = newLine.GetOffsetCurves(-pipeConnection.Diameter / 2)[0] as Line;
                    offsetMinus.Layer = Constants.JPP_D_PipeWalls;

                    acBlkTblRec.AppendEntity(newLine);
                    tr.AddNewlyCreatedDBObject(newLine, true);

                    acBlkTblRec.AppendEntity(offsetMinus);
                    tr.AddNewlyCreatedDBObject(offsetMinus, true);
                    acBlkTblRec.AppendEntity(offsetPlus);
                    tr.AddNewlyCreatedDBObject(offsetPlus, true);
                }

                Circle innerManhole = new Circle(location, Vector3d.ZAxis, (double)(Diameter / 2));
                innerManhole.Layer = Constants.JPP_D_ManholeWall;
                acBlkTblRec.AppendEntity(innerManhole);
                tr.AddNewlyCreatedDBObject(innerManhole, true);

                Circle outerManhole = new Circle(location, Vector3d.ZAxis, (double)(Diameter / 2) + 50f);
                outerManhole.Layer = Constants.JPP_D_ManholeWall;
                acBlkTblRec.AppendEntity(outerManhole);
                tr.AddNewlyCreatedDBObject(outerManhole, true);

                Circle outerSurround = new Circle(location, Vector3d.ZAxis, (double)(Diameter / 2) + 200f);
                outerSurround.Layer = Constants.JPP_D_ManholeWall;
                acBlkTblRec.AppendEntity(outerSurround);
                tr.AddNewlyCreatedDBObject(outerSurround, true);

                tr.Commit();
            }
        }

        [CommandMethod("C_D_AddPlan")]
        public static void AddPlan()
        {
            Manhole current = new Manhole();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter manhole diameter: ");
            pStrOpts.AllowSpaces = false;
            current.Diameter = int.Parse(acDoc.Editor.GetString(pStrOpts).StringResult);

            pStrOpts = new PromptStringOptions("\nEnter manhole cover level: ");
            pStrOpts.AllowSpaces = false;
            current.CoverLevel = float.Parse(acDoc.Editor.GetString(pStrOpts).StringResult);

            pStrOpts = new PromptStringOptions("\nEnter manhole invert level: ");
            pStrOpts.AllowSpaces = false;
            current.InvertLevel = float.Parse(acDoc.Editor.GetString(pStrOpts).StringResult);

            // Prompt for the start point
            PromptPointResult pPtRes;
            PromptPointOptions pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nPlease click the pipe intersection point: ";
            var temp3d = acDoc.Editor.GetPoint(pPtOpts).Value;
            current.IntersectionPoint = new Point3d(temp3d.X, temp3d.Y, 0);

            bool processing = true;
            while (processing)
            {
                PipeConnection pc = new PipeConnection();
                pPtOpts = new PromptPointOptions("");
                pPtOpts.Message = "\nPlease click the pipe intersection point: ";
                var locationResult = acDoc.Editor.GetPoint(pPtOpts);
                if (locationResult.Status != PromptStatus.OK)
                {
                    processing = false;
                    break;
                }
                temp3d = locationResult.Value;
                pc.Location = new Point3d(temp3d.X, temp3d.Y, 0);

                pStrOpts = new PromptStringOptions("\nEnter pipe diameter: ");
                pStrOpts.AllowSpaces = false;
                var diameterResult = acDoc.Editor.GetString(pStrOpts);
                if (diameterResult.Status != PromptStatus.OK)
                {
                    processing = false;
                    break;
                }
                pc.Diameter = int.Parse(diameterResult.StringResult);

                current.IncomingPipes.Add(pc);
            }

            current.outgoingConnection = new PipeConnection();
            pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nPlease click the outgoing pipe intersection point: ";
            var outgoingLocationResult = acDoc.Editor.GetPoint(pPtOpts);
            if (outgoingLocationResult.Status != PromptStatus.OK)
            {
                throw new NotImplementedException();
            }
            temp3d = outgoingLocationResult.Value;
            current.outgoingConnection.Location = new Point3d(temp3d.X, temp3d.Y, 0);

            pStrOpts = new PromptStringOptions("\nEnter outgoing pipe diameter: ");
            pStrOpts.AllowSpaces = false;
            var outgoingDiameterResult = acDoc.Editor.GetString(pStrOpts);
            if (outgoingDiameterResult.Status != PromptStatus.OK)
            {
                throw new NotImplementedException();
            }
            current.outgoingConnection.Diameter = int.Parse(outgoingDiameterResult.StringResult);

            pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nPlease click the outgoing pipe intersection point: ";
            var planLocationResult = acDoc.Editor.GetPoint(pPtOpts);
            if (planLocationResult.Status != PromptStatus.OK)
            {
                throw new NotImplementedException();
            }
            temp3d = planLocationResult.Value;

            try
            {
                current.GeneratePlan(new Point3d(temp3d.X, temp3d.Y, 0));
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

    class PipeConnection
    {
        public int Diameter { get; set; }
        public Point3d Location { get; set; }
    }
}
