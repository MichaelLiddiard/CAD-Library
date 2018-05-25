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
using JPP.Core;
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
            //Sort pipe connections for ease of drawing
            var sortedPipes = from p in IncomingPipes orderby p.Angle ascending select p;

            DrainageNetwork.Current.Standard.VerifyManhole(this);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                Vector3d offset = location.GetAsVector().Subtract(IntersectionPoint.GetAsVector());

                //Calculate alternatce connection point
                Circle slopeCircle = new Circle(location, Vector3d.ZAxis, 450);
                Vector3d slopeIntersect = location.GetVectorTo(outgoingConnection.Location.Add(offset));
                slopeIntersect = slopeIntersect * 450 / slopeIntersect.Length;
                Point2d slopePoint2D = new Point2d(location.Add(slopeIntersect).X, location.Add(slopeIntersect).Y);

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

                //Outgoin line
                Polyline outgoingLine = new Polyline();
                outgoingLine.AddVertexAt(0, new Point2d(location.X, location.Y), 0, 0, 0);
                outgoingLine.AddVertexAt(1, new Point2d(outgoingConnection.Location.Add(offset).X, outgoingConnection.Location.Add(offset).Y), 0, 0, 0);
                outgoingLine.Layer = Constants.JPP_D_PipeCentreline;

                Polyline outgoingoffsetPlus = outgoingLine.GetOffsetCurves(outgoingConnection.Diameter / 2)[0] as Polyline;
                outgoingoffsetPlus.Layer = Constants.JPP_D_PipeWalls;
                Polyline outgoingoffsetMinus = outgoingLine.GetOffsetCurves(-outgoingConnection.Diameter / 2)[0] as Polyline;
                outgoingoffsetMinus.Layer = Constants.JPP_D_PipeWalls;

                acBlkTblRec.AppendEntity(outgoingLine);
                tr.AddNewlyCreatedDBObject(outgoingLine, true);

                acBlkTblRec.AppendEntity(outgoingoffsetMinus);
                tr.AddNewlyCreatedDBObject(outgoingoffsetMinus, true);
                acBlkTblRec.AppendEntity(outgoingoffsetPlus);
                tr.AddNewlyCreatedDBObject(outgoingoffsetPlus, true);

                Polyline lastLine = outgoingoffsetPlus;

                for (int i = 0; i < sortedPipes.Count(); i++)
                {
                    PipeConnection pipeConnection = sortedPipes.ToArray()[i];       
                    
                    Polyline newLine = new Polyline();//location, pipeConnection.Location.Add(offset)
                    newLine.AddVertexAt(0, new Point2d(location.X, location.Y), 0, 0, 0);
                    newLine.AddVertexAt(1, new Point2d(pipeConnection.Location.Add(offset).X, pipeConnection.Location.Add(offset).Y), 0, 0, 0);
                    newLine.Layer = Constants.JPP_D_PipeCentreline;

                    //Check that angle is ok
                    if (pipeConnection.Angle < 135 || pipeConnection.Angle > 225)
                    {
                        //Angle exceeds 45° so change
                        Point3dCollection  slopeIntersectCollection = new Point3dCollection();
                        newLine.IntersectWith(slopeCircle, Intersect.ExtendArgument, slopeIntersectCollection, IntPtr.Zero, IntPtr.Zero);
                        Point3d circleIntersectPoint = slopeIntersectCollection[0];
                        newLine.AddVertexAt(1, new Point2d(circleIntersectPoint.X, circleIntersectPoint.Y), 0,0,0 );

                        newLine.SetPointAt(0, slopePoint2D);
                    }

                    Polyline offsetPlus = newLine.GetOffsetCurves(pipeConnection.Diameter / 2)[0] as Polyline;
                    offsetPlus.Layer = Constants.JPP_D_PipeWalls;
                    Polyline offsetMinus = newLine.GetOffsetCurves(-pipeConnection.Diameter / 2)[0] as Polyline;
                    offsetMinus.Layer = Constants.JPP_D_PipeWalls;

                    //Fillet
                    Point3dCollection collection = new Point3dCollection();
                    offsetMinus.IntersectWith(lastLine, Intersect.ExtendBoth, collection, IntPtr.Zero, IntPtr.Zero);

                    Point3d Intersection = collection[0];
                    lastLine.SetPointAt(0, new Point2d(Intersection.X, Intersection.Y));
                    offsetMinus.SetPointAt(0, new Point2d(Intersection.X, Intersection.Y));

                    Arc a = lastLine.Fillet(offsetMinus, 50);

                    acBlkTblRec.AppendEntity(newLine);
                    tr.AddNewlyCreatedDBObject(newLine, true);

                    acBlkTblRec.AppendEntity(offsetMinus);
                    tr.AddNewlyCreatedDBObject(offsetMinus, true);
                    acBlkTblRec.AppendEntity(offsetPlus);
                    tr.AddNewlyCreatedDBObject(offsetPlus, true);
                    acBlkTblRec.AppendEntity(a);
                    tr.AddNewlyCreatedDBObject(a, true);

                    lastLine = offsetPlus;

                }

                Point3dCollection lastCollection = new Point3dCollection();
                outgoingoffsetMinus.IntersectWith(lastLine, Intersect.ExtendBoth, lastCollection, IntPtr.Zero, IntPtr.Zero);

                if (lastCollection.Count > 0)
                {
                    Point3d lastIntersection = lastCollection[0]; //No intersection throwing error???
                    lastLine.SetPointAt(0, new Point2d(lastIntersection.X, lastIntersection.Y));
                    outgoingoffsetMinus.SetPointAt(0, new Point2d(lastIntersection.X, lastIntersection.Y));
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

            Vector3d outgoing = current.IntersectionPoint.GetVectorTo(current.outgoingConnection.Location);

            bool processing = true;
            while (processing)
            {
                PipeConnection pc = new PipeConnection();
                pPtOpts = new PromptPointOptions("");
                pPtOpts.Message = "\nPlease click the pipe intersection point, or press escape if done: ";
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

                Vector3d line = current.IntersectionPoint.GetVectorTo(pc.Location);
                pc.Angle = line.GetAngleTo(outgoing, Vector3d.ZAxis) * 180 / Math.PI;

                current.IncomingPipes.Add(pc);
            }

            pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nPlease click the location to insert plan detail: ";
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
        public double Angle { get; set; }
    }
}
