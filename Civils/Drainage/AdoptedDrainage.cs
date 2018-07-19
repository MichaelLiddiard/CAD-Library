using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JPP.Civils.Highways;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(JPP.Civils.AdoptedDrainage))]

namespace JPP.Civils
{
    class AdoptedDrainage
    {
        public GridArray<DrainageNode> adoptedStormManholes { get; set; }
        public GridArray<DrainageNode> adoptedFoulManholes { get; set; }

        public AdoptedDrainage()
        {
            adoptedFoulManholes = new GridArray<DrainageNode>();
            adoptedStormManholes = new GridArray<DrainageNode>();
        }

        public void CalculateAdoptedDrainage(ObservableCollection<Road> roadNetwork)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartOpenCloseTransaction())
            {

                //Determine road junctions and add manhole at intersections

                //For each road segment                        
                foreach (Road r in roadNetwork)
                {
                    Curve centreline = tr.GetObject(r.Centreline, OpenMode.ForRead) as Curve;
                    DBObjectCollection kerbs = new DBObjectCollection();

                    var offset = centreline.GetOffsetCurves(r.OverallWidth / 2);
                    foreach(DBObject db in offset)
                    {
                        kerbs.Add(db);
                    }
                    offset = centreline.GetOffsetCurves(-r.OverallWidth / 2);
                    foreach (DBObject db in offset)
                    {
                        kerbs.Add(db);
                    }

                    DBObjectCollection parts = new DBObjectCollection();
                    centreline.Explode(parts);

                    foreach (DBObject obj in parts)
                    {
                        //Place manholes at end of straight sections
                        if(obj is Line)
                        {
                            Line l = obj as Line;
                            //TODO: CHeck this number works
                            Curve c = l.GetOffsetCurves(r.OverallWidth / 2 - 0.75 - 0.5)[0] as Curve;

                            AddManholes(c);

                        }

                        //Place manholes at curve ends, and try straigtening
                        if(obj is Arc)
                        {
                            Arc a = obj as Arc;
                            Curve c = a.GetOffsetCurves(r.OverallWidth / 2 - 0.75 - 0.5)[0] as Curve;

                            AddManholes(c);
                        }
                    }
                }

                foreach(DrainageNode dn in adoptedStormManholes)
                {
                    DrawNode(dn);
                }

                tr.Commit();
            }
        }  
        
        private void AddManholes(Curve c, DrainageNode previous)
        {
            DrainageNode dn = new DrainageNode();
            dn.X = (int)Math.Round(c.StartPoint.X);
            dn.Y = (int)Math.Round(c.StartPoint.Y);
            if(previous != null)
            {
                dn.Connections.Add(previous);
            }

            if (adoptedStormManholes[dn.X, dn.Y] == null)
            {
                adoptedStormManholes[dn.X, dn.Y] = dn;
            }

            DrainageNode dn2 = new DrainageNode();
            dn2.X = (int)Math.Round(c.EndPoint.X);
            dn2.Y = (int)Math.Round(c.EndPoint.Y);

            if (adoptedStormManholes[dn2.X, dn2.Y] == null)
            {
                adoptedStormManholes[dn2.X, dn2.Y] = dn2;
            }
        }

        //TODO: Remove
        private void DrawNode(Node n)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
            {

                Circle c = new Circle();
                c.Diameter = 0.5f;
                c.Center = new Point3d(n.X, n.Y, 0);

                // Open the Block table for read
                BlockTable acBlkTbl = trans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                acBlkTblRec.AppendEntity(c);
                trans.AddNewlyCreatedDBObject(c, true);

                trans.Commit();
            }

            acDoc.TransactionManager.EnableGraphicsFlush(true);
            acDoc.TransactionManager.QueueForGraphicsFlush();
            Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
        }


        [CommandMethod("C_D_GenerateAdoptedNetwork")]
        public static void GenerateAdopetedNetworkCommand()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            var roads = acDoc.GetDocumentStore<CivilDocumentStore>().Roads;

            AdoptedDrainage ad = new AdoptedDrainage();
            ad.CalculateAdoptedDrainage(roads);
        }
    }
}
