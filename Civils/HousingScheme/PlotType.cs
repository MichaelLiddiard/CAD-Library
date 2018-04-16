using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using JPP.Core;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: CommandClass(typeof(JPP.Civils.PlotType))]

namespace JPP.Civils
{
    //TODO: Add code to check if a plot type is not currently open, and gracefully handle prompts being cancelled
    public class PlotType
    {
        public static PlotType CurrentOpen;

        public string PlotTypeName { get; set; }

        public List<AccessPoint> AccessPoints;

        public long PerimeterLinePtr;

        [XmlIgnore]
        public ObjectId PerimeterLine
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(PerimeterLinePtr), 0);
            }
            set
            {
                PerimeterLinePtr = value.Handle.Value;
            }
        }

        public long BackgroundBlockIDPtr;

        [XmlIgnore]
        public ObjectId BackgroundBlockID
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(BackgroundBlockIDPtr), 0);
            }
            set
            {
                BackgroundBlockIDPtr = value.Handle.Value;
            }
        }

        public long BlockIDPtr;

        [XmlIgnore]
        public ObjectId BlockID
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(BlockIDPtr), 0);
            }
            set
            {
                BlockIDPtr = value.Handle.Value;
            }
        }

        public long BasepointPtr;

        [XmlIgnore]
        public ObjectId BasepointID
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(BasepointPtr), 0);
            }
            set
            {
                BasepointPtr = value.Handle.Value;
            }
        }

        public Point3d BasePoint;

        public List<WallSegment> Segments;

        public PlotType()
        {
            AccessPoints = new List<AccessPoint>();
            Segments = new List<WallSegment>();
        }

        public void DefineBlock()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;

            BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr;
            ObjectId newBlockId;

            if (bt.Has(PlotTypeName))
            {
                newBlockId = bt[PlotTypeName];
                btr = tr.GetObject(newBlockId, OpenMode.ForWrite) as BlockTableRecord;
                foreach (ObjectId e in btr)
                {
                    Entity temp = tr.GetObject(e, OpenMode.ForWrite) as Entity;
                    temp.Erase();
                }

            }
            else
            {
                bt.UpgradeOpen();
                btr = new BlockTableRecord();
                btr.Name = PlotTypeName;
                btr.Origin = BasePoint;
                newBlockId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
            }

            ObjectIdCollection plotObjects = new ObjectIdCollection();
            plotObjects.Add(PerimeterLine);

            // Copy the entities to the block using deepclone
            IdMapping acMapping = new IdMapping();
            acCurDb.DeepCloneObjects(plotObjects, newBlockId, acMapping, false);

            BlockID = btr.ObjectId;
        }

        [CommandMethod("PT_Create")]
        public static void CreatePlotType()
        {          
            Document acDoc = Application.DocumentManager.MdiActiveDocument;                     

            PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter plot type name: ");
            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);

            // Prompt for the start point
            PromptPointResult pPtRes;
            PromptPointOptions pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nEnter the base point. Basepoint to be located at bottom left corner of the plot: ";
            pPtRes = acDoc.Editor.GetPoint(pPtOpts);

            PlotType.CurrentOpen = new PlotType() { PlotTypeName = pStrRes.StringResult, BasePoint = pPtRes.Value };

            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                //Create all plot specific layers
                LayerTable acLayerTable = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;
                Core.Utilities.CreateLayer(tr, acLayerTable, Constants.JPP_HS_PlotPerimiter, Constants.JPP_HS_PlotPerimiterColor);

                BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr;

                bt.UpgradeOpen();
                btr = new BlockTableRecord();
                btr.Name = PlotType.CurrentOpen.PlotTypeName + "Background";
                btr.Origin = PlotType.CurrentOpen.BasePoint;
                var objRef = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);                

                BlockTableRecord btr2 = new BlockTableRecord();
                btr2.Name = PlotType.CurrentOpen.PlotTypeName;
                btr2.Origin = PlotType.CurrentOpen.BasePoint;
                var blockRef = bt.Add(btr2);
                tr.AddNewlyCreatedDBObject(btr2, true);

                PlotType.CurrentOpen.BackgroundBlockID = Core.Utilities.InsertBlock(PlotType.CurrentOpen.BasePoint, 0, objRef);
                PlotType.CurrentOpen.BlockID = blockRef;

                //Add basepoint
                Circle bp = new Circle();
                bp.Center = PlotType.CurrentOpen.BasePoint;
                bp.Radius = 0.5f;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                PlotType.CurrentOpen.BasepointID = acBlkTblRec.AppendEntity(bp);
                tr.AddNewlyCreatedDBObject(bp, true);

                tr.Commit();
            }       

            /*
            JPPCommands.JPPCommandsInitialisation.JPPCommandsInitialise();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter plot type name: ");
            pStrOpts.AllowSpaces = true;            
            PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);
            if (pStrRes.Status == PromptStatus.OK)
            {
                PlotType pt = new PlotType();
                pt.PlotTypeName = pStrRes.StringResult;

                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    pt.PerimeterLine = AddFFL.CreateOutline();
                    AddFFL.FormatOutline(pt.PerimeterLine);
                    tr.Commit();
                }
                acDoc.Editor.Regen();
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    //Get the base point
                    Curve pl = tr.GetObject(pt.PerimeterLine, OpenMode.ForRead) as Curve;
                    pt.BasePoint = pl.GetPointAtDist(0);

                    //Define the access points
                    Editor acEditor = acDoc.Editor;
                    // Prompt user to click on access points.
                    bool addingAccessPoints = true;
                    PromptPointOptions promptAccessPtOpts = new PromptPointOptions("\nClick access point or Spacebar when done: ");
                    promptAccessPtOpts.AllowNone = true;
                    // Set up prompt string
                    PromptStringOptions promptQuestionOpts = new PromptStringOptions("\nIs this access point at FFL (Y/N)?");
                    // Loop while adding access points
                    while (addingAccessPoints)
                    {
                        PromptPointResult promptResult = acEditor.GetPoint(promptAccessPtOpts);
                        if (promptResult.Status == PromptStatus.OK)
                        {
                            // Prompt user for access point level
                            PromptResult promptStringResult = acEditor.GetString(promptQuestionOpts);
                            AccessPoint ap = new AccessPoint();
                            switch (promptStringResult.StringResult.ToUpper())
                            {
                                case "Y":
                                    ap.Offset = 0;
                                    break;
                                case "N":
                                    ap.Offset = -0.150;
                                    break;
                                default:
                                    acEditor.WriteMessage("\nInvalid input. Access point level set at FFL!");
                                    ap.Offset = 0;
                                    break;
                            }
                            ap.Parameter = pl.GetParameterAtPoint(pl.GetClosestPointTo(promptResult.Value, false));
                            pt.AccessPoints.Add(ap);
                        }
                        else if (promptResult.Status == PromptStatus.None)
                        {
                            addingAccessPoints = false;
                        }
                    }
                    pt.DefineBlock();

                    tr.Commit();
                }
            

                CivilDocumentStore cds = acDoc.GetDocumentStore<CivilDocumentStore>();
                cds.PlotTypes.Add(pt);                
            }*/
        }

        

        [CommandMethod("PT_CreateWS")]
        public static void CreateWallSegments()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // All work will be done in the WCS so save the current UCS
            // to restore later and set the UCS to WCS
            Matrix3d CurrentUCS = acEditor.CurrentUserCoordinateSystem;
            acEditor.CurrentUserCoordinateSystem = Matrix3d.Identity;

            // Get the current color, for temp graphics
            // Color currCol = acCurrDb.Cecolor;
            Color drawColor = Color.FromColorIndex(ColorMethod.ByAci, 1);
            // Create a 3d point collection to store the vertices 
            Point3dCollection PickPts = new Point3dCollection();

            // Set up the selection options
            PromptPointOptions promptCornerPtOpts = new PromptPointOptions("\nSelect points along segment: ");
            promptCornerPtOpts.AllowNone = true;

            // Get the start point for the polyline
            PromptPointResult promptResult = acEditor.GetPoint(promptCornerPtOpts);
            // Continue to add picked corner points to the polyline
            while (promptResult.Status == PromptStatus.OK)
            {
                // Add the selected point PickPts collection
                PickPts.Add(promptResult.Value);
                // Drag a temp line during selection of subsequent points
                promptCornerPtOpts.UseBasePoint = true;
                promptCornerPtOpts.BasePoint = promptResult.Value;
                promptResult = acEditor.GetPoint(promptCornerPtOpts);
                if (promptResult.Status == PromptStatus.OK)
                {
                    // For each point selected, draw a temporary segment
                    acEditor.DrawVector(PickPts[PickPts.Count - 1],     // start point
                                    promptResult.Value,                 // end point
                                    drawColor.ColorIndex,               // highlight colour
                                                                        //currCol.ColorIndex,               // current color
                                    false);                             // highlighted
                }
            }

            Polyline acPline = new Polyline(PickPts.Count);
            acPline.Layer = Constants.JPP_HS_PlotPerimiter;
            // The user has pressed SPACEBAR to exit the picking points loop
            if (promptResult.Status == PromptStatus.None)
            {
                foreach (Point3d pt in PickPts)
                {
                    // Alert user that picked point has elevation.
                    if (pt.Z != 0.0)
                        acEditor.WriteMessage("/nWarning: corner point has non-zero elevation. Elevation will be ignored.");
                    acPline.AddVertexAt(acPline.NumberOfVertices, new Point2d(pt.X, pt.Y), 0, 0, 0);
                }
                // If user has clicked the start point to close the polyline delete this point and
                // set polyline to closed
                if (acPline.EndPoint == acPline.StartPoint)
                {
                    acPline.RemoveVertexAt(acPline.NumberOfVertices - 1);
                    acPline.Closed = true;
                }
            }

            //Explode the line and create wall segments to match
            DBObjectCollection lineSegments = new DBObjectCollection();
            acPline.Explode(lineSegments);

            using (Transaction tr = acDoc.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (DBObject dbobj in lineSegments)
                {
                    Entity e = dbobj as Entity;                                       

                    WallSegment ws = new WallSegment() { PerimeterLine = acBlkTblRec.AppendEntity(e), Guid = Guid.NewGuid().ToString() };
                    e.XData = new ResultBuffer(new TypedValue(1001, "JPP"), new TypedValue(1000, ws.Guid));
                    tr.AddNewlyCreatedDBObject(e, true);

                    PlotType.CurrentOpen.Segments.Add(ws);
                }

                tr.Commit();                
            }
        }

        [CommandMethod("PT_Add")]
        public static void AddToBackground()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr;
                ObjectId newBlockId;

                newBlockId = bt[PlotType.CurrentOpen.PlotTypeName + "Background"];
                btr = tr.GetObject(newBlockId, OpenMode.ForWrite) as BlockTableRecord;

                ObjectIdCollection plotObjects = new ObjectIdCollection();

                //Select objects to be added
                PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();
                SelectionSet acSSet = acSSPrompt.Value;
                foreach (SelectedObject acSSObj in acSSet)
                {
                    plotObjects.Add(acSSObj.ObjectId);
                }

                // Copy the entities to the block using deepclone
                IdMapping acMapping = new IdMapping();
                acCurDb.DeepCloneObjects(plotObjects, newBlockId, acMapping, false);

                foreach (ObjectId oid in plotObjects)
                {
                    DBObject e = tr.GetObject(oid, OpenMode.ForWrite);
                    e.Erase();
                }

                tr.Commit();
            }

            //Triggeer regen to update blocks display
            //alternatively http://adndevblog.typepad.com/autocad/2012/05/redefining-a-block.html
            Application.DocumentManager.CurrentDocument.Editor.Regen();
        }

        [CommandMethod("PT_AddAccess")]
        public static void AddAccessPointd()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter the access point level realtive to ground: ");
            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);

            // Prompt for the start point
            PromptPointResult pPtRes;
            PromptPointOptions pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nEnter the access point. Access point to fall on wall segment: ";
            pPtRes = acDoc.Editor.GetPoint(pPtOpts);                       

            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                //Split the matching wall segment
                int deletionIndex = 0;
                bool found = false;
                for(int i = 0; i < PlotType.CurrentOpen.Segments.Count; i++)
                {
                    Line segment = tr.GetObject(PlotType.CurrentOpen.Segments[i].PerimeterLine, OpenMode.ForRead) as Line;
                    if(segment.GetGeCurve().IsOn(pPtRes.Value))
                    {
                        deletionIndex = i;
                        found = true;
                    }                    
                }

                if (found)
                {
                    var result = PlotType.CurrentOpen.Segments[deletionIndex].Split(pPtRes.Value);
                    PlotType.CurrentOpen.Segments.AddRange(result);
                    PlotType.CurrentOpen.Segments[deletionIndex].Erase();
                    PlotType.CurrentOpen.Segments.RemoveAt(deletionIndex);

                    //Add the access point
                    BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr;
                    ObjectId newBlockId;

                    newBlockId = bt[PlotType.CurrentOpen.PlotTypeName + "Background"];
                    btr = tr.GetObject(newBlockId, OpenMode.ForWrite) as BlockTableRecord;

                    ObjectIdCollection plotObjects = new ObjectIdCollection();

                    //Add basepoint
                    Circle accessPointCircle = new Circle();
                    accessPointCircle.Layer = Constants.JPP_HS_PlotPerimiter; //TODO: move to another layer??
                    accessPointCircle.Center = pPtRes.Value;
                    accessPointCircle.Radius = 0.25f;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    plotObjects.Add(acBlkTblRec.AppendEntity(accessPointCircle));
                    tr.AddNewlyCreatedDBObject(accessPointCircle, true);

                    // Copy the entities to the block using deepclone
                    IdMapping acMapping = new IdMapping();
                    acCurDb.DeepCloneObjects(plotObjects, newBlockId, acMapping, false);

                    foreach (ObjectId oid in plotObjects)
                    {
                        DBObject e = tr.GetObject(oid, OpenMode.ForWrite);
                        e.Erase();
                    }

                    PlotType.CurrentOpen.AccessPoints.Add(new AccessPoint() { Location = pPtRes.Value, Offset = float.Parse(pStrRes.StringResult) });

                    tr.Commit();
                } else
                {
                    //TODO: say why failed.
                }
            }

            //Triggeer regen to update blocks display
            //alternatively http://adndevblog.typepad.com/autocad/2012/05/redefining-a-block.html
            Application.DocumentManager.CurrentDocument.Editor.Regen();
        }

        [CommandMethod("PT_Finalise")]
        public static void Finalise()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr;
                ObjectId newBlockId;


                newBlockId = bt[PlotType.CurrentOpen.PlotTypeName];
                btr = tr.GetObject(newBlockId, OpenMode.ForWrite) as BlockTableRecord;                
                foreach (ObjectId e in btr)
                {
                    Entity temp = tr.GetObject(e, OpenMode.ForWrite) as Entity;
                    temp.Erase();
                }

                ObjectIdCollection plotObjects = new ObjectIdCollection();

                plotObjects.Add(PlotType.CurrentOpen.BackgroundBlockID);
                plotObjects.Add(PlotType.CurrentOpen.BasepointID);

                foreach(WallSegment ws in PlotType.CurrentOpen.Segments)
                {
                    plotObjects.Add(ws.PerimeterLine);
                }

                btr.AssumeOwnershipOf(plotObjects);

                tr.Commit();
            }

            //Triggeer regen to update blocks display
            //alternatively http://adndevblog.typepad.com/autocad/2012/05/redefining-a-block.html
            Application.DocumentManager.CurrentDocument.Editor.Regen();

            //Add to the document store
            CivilDocumentStore cds = acDoc.GetDocumentStore<CivilDocumentStore>();
            cds.PlotTypes.Add(PlotType.CurrentOpen);

            PlotType.CurrentOpen = null;
        }
    }

    public struct AccessPoint
    {
        public Point3d Location;
        public double Offset;
    }
}
