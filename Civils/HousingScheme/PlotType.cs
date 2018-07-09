using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(JPP.Civils.PlotType))]

namespace JPP.Civils
{
    public class PlotType : ILibraryItem, ICloneable
    {
        public static PlotType CurrentOpen;
        public delegate void CurrentOpenDelegate();
        public static event CurrentOpenDelegate OnCurrentOpenChanged;

        // ReSharper disable once MemberCanBePrivate.Global
        public string PlotTypeName { get; set; }
        
        // ReSharper disable once FieldCanBeMadeReadOnly.Global as needed to be serialized
        public List<AccessPoint> AccessPoints;
        
        // ReSharper disable once MemberCanBePrivate.Global as needed to be serialized
        public long PerimeterLinePtr;

        [XmlIgnore] public Database acCurDb;

        [XmlIgnore]
        public ObjectId PerimeterLine
        {
            get
            {
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
                return acCurDb.GetObjectId(false, new Handle(BasepointPtr), 0);
            }
            set
            {
                BasepointPtr = value.Handle.Value;
            }
        }

        public PersistentObjectIdCollection AccessPointLocations;

        public Point3d BasePoint;

        public List<WallSegment> Segments;

        /// <summary>
        /// Create new blank plot type
        /// </summary>
        public PlotType()
        {
            AccessPoints = new List<AccessPoint>();
            AccessPointLocations = new PersistentObjectIdCollection();
            Segments = new List<WallSegment>();
        }

        /// <summary>
        /// Autocad command method for creation of plot type. Prompts user for name and basepoint, and then creates a new plot type and set it to the current active one.
        /// </summary>
        [CommandMethod("PT_Create")]
        public static void CreatePlotType()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Editor acEditor = acDoc.Editor;
            Database acCurDb = acDoc.Database;

            if (CurrentOpen == null)
            {
                PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter plot type name: ")
                {
                    AllowSpaces = true
                };
                PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);

                //Verify input
                if (pStrRes.Status == PromptStatus.OK)
                {
                    //Check the block does not already exist
                    bool exists = false;
                    using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                    {
                        //Create all plot specific layers
                        LayerTable acLayerTable = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;
                        Core.Utilities.CreateLayer(Constants.JPP_HS_PlotPerimiter, Constants.JPP_HS_PlotPerimiterColor);

                        //Create the background block
                        BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                        if (bt.Has(pStrRes.StringResult))
                        {
                            exists = true;
                        }
                    }

                    if (!exists)
                    {

                        // Prompt for the start point
                        PromptPointOptions pPtOpts = new PromptPointOptions("")
                        {
                            Message = "\nEnter the base point. Basepoint to be located at bottom left corner of the plot: "
                        };
                        PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);

                        if (pPtRes.Status == PromptStatus.OK)
                        {

                            CurrentOpen = new PlotType()
                            {
                                PlotTypeName = pStrRes.StringResult,
                                BasePoint = pPtRes.Value
                            };

                            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                            {
                                Main.AddRegAppTableRecord();

                                //Create all plot specific layers
                                LayerTable acLayerTable = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;
                                Core.Utilities.CreateLayer(Constants.JPP_HS_PlotPerimiter, Constants.JPP_HS_PlotPerimiterColor);

                                //Create the background block
                                BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                                bt.UpgradeOpen();

                                BlockTableRecord backgroundBlockRecord = new BlockTableRecord
                                {
                                    Name = CurrentOpen.PlotTypeName + "Background",
                                    Origin = CurrentOpen.BasePoint
                                };
                                ObjectId objRef = bt.Add(backgroundBlockRecord);
                                tr.AddNewlyCreatedDBObject(backgroundBlockRecord, true);

                                //Prep the block for finalising
                                BlockTableRecord plotTypeBlockRecord = new BlockTableRecord
                                {
                                    Name = CurrentOpen.PlotTypeName,
                                    Origin = CurrentOpen.BasePoint
                                };
                                ObjectId blockRef = bt.Add(plotTypeBlockRecord);
                                tr.AddNewlyCreatedDBObject(plotTypeBlockRecord, true);

                                //Insert the background block
                                CurrentOpen.BackgroundBlockID = Core.Utilities.InsertBlock(CurrentOpen.BasePoint, 0, objRef);
                                CurrentOpen.BlockID = blockRef;

                                //Create and add basepoint
                                Circle bp = new Circle
                                {
                                    Center = CurrentOpen.BasePoint,
                                    Radius = 0.5f
                                };
                                BlockTableRecord acBlkTblRec = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                                if (acBlkTblRec != null)
                                {
                                    CurrentOpen.BasepointID = acBlkTblRec.AppendEntity(bp);
                                }
                                else
                                {
                                    //This should never, ever come up but best to handle it
                                    throw new NullReferenceException("Model space not found", null);
                                }

                                tr.AddNewlyCreatedDBObject(bp, true);

                                tr.Commit();
                            }

                            //Inform all event handlers the current plot type has changed
                            OnCurrentOpenChanged?.Invoke();
                        }
                        else
                        {
                            acEditor.WriteMessage("Point selection cancelled\n");
                        }
                    }
                    else
                    {
                        acEditor.WriteMessage("Plot Type Name already exists as block. Please choose a different name or rename exisitng block\n");
                    }
                }
                else
                {
                    acEditor.WriteMessage("No plot type name entered\n");
                }
            }
            else
            {
                acEditor.WriteMessage("Plot Type already open for editing. Please finalise before attempting to create a new plot type.\n");
            }
        }

        //TODO: Review
        [CommandMethod("PT_CreateWS")]
        public static void CreateWallSegments()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            if (CurrentOpen != null)
            {
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
                        //TODO: Rework this using the transient api so they dont disappear on move
                        // For each point selected, draw a temporary segment
                        acEditor.DrawVector(PickPts[PickPts.Count - 1], // start point
                            promptResult.Value, // end point
                            drawColor.ColorIndex, // highlight colour
                            //currCol.ColorIndex,               // current color
                            false); // highlighted
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
                        Line e = dbobj as Line;

                        WallSegment ws = new WallSegment() {PerimeterLine = acBlkTblRec.AppendEntity(e), Guid = Guid.NewGuid().ToString()};
                        e.XData = new ResultBuffer(new TypedValue(1001, "JPP"), new TypedValue(1000, ws.Guid));
                        tr.AddNewlyCreatedDBObject(e, true);

                        PlotType.CurrentOpen.WSIntersect(e.StartPoint);
                        PlotType.CurrentOpen.WSIntersect(e.EndPoint);

                        PlotType.CurrentOpen.Segments.Add(ws);
                    }

                    tr.Commit();
                }
            }
            else
            {
                acEditor.WriteMessage("No Plot Type currently open\n");
            }
        }

        [CommandMethod("PT_Add")]
        public static void AddToBackground()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            if (CurrentOpen != null)
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {

                    BlockTable bt = (BlockTable) tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                    //get the backgroung block
                    ObjectId backgroundBlock = bt[CurrentOpen.PlotTypeName + "Background"];

                    ObjectIdCollection objectsToTransfer = new ObjectIdCollection();

                    //Select objects to be added
                    PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();
                    SelectionSet acSSet = acSSPrompt.Value;
                    foreach (SelectedObject acSSObj in acSSet)
                    {
                        objectsToTransfer.Add(acSSObj.ObjectId);
                    }

                    // Copy the entities to the block using deepclone
                    IdMapping acMapping = new IdMapping();
                    acCurDb.DeepCloneObjects(objectsToTransfer, backgroundBlock, acMapping, false);

                    foreach (ObjectId oid in objectsToTransfer)
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
            else
            {
                acEditor.WriteMessage("No Plot Type currently open\n");
            }
        }

        [CommandMethod("PT_AddAccess")]
        public static void AddAccessPointd()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            if (CurrentOpen != null)
            {
                //TODO: Add transient graphics and error checking

                PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter the access point level realtive to floor level: ")
                {
                    AllowSpaces = true
                };
                PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);

                // Prompt for the start point
                PromptPointOptions pPtOpts = new PromptPointOptions("")
                {
                    Message = "\nEnter the access point. Access point to fall on wall segment: "
                };
                PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);
                
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    if (PlotType.CurrentOpen.WSIntersect(pPtRes.Value))
                    {

                        //Add the access point
                        BlockTable bt = (BlockTable) tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);

                        ObjectId newBlockId = bt[PlotType.CurrentOpen.PlotTypeName + "Background"];

                        ObjectIdCollection plotObjects = new ObjectIdCollection();

                        //Add basepoint
                        Circle accessPointCircle = new Circle
                        {
                            Layer = Constants.JPP_HS_PlotPerimiter, //TODO: move to another layer??
                            Center = pPtRes.Value,
                            Radius = 0.25f
                        };

                        // Open the Block table record Model space for write
                        BlockTableRecord acBlkTblRec = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                        ObjectId temp = acBlkTblRec.AppendEntity(accessPointCircle);
                        PlotType.CurrentOpen.AccessPointLocations.Add(temp);
                        tr.AddNewlyCreatedDBObject(accessPointCircle, true);

                        string id = System.Guid.NewGuid().ToString();
                        accessPointCircle.XData = new ResultBuffer(new TypedValue(1001, "JPP"), new TypedValue(1000, id));
                        PlotType.CurrentOpen.AccessPoints.Add(new AccessPoint() {Location = pPtRes.Value, Offset = float.Parse(pStrRes.StringResult), Guid = id});

                        tr.Commit();
                    }
                    else
                    {
                        //TODO: say why failed.
                    }
                }

                //Triggeer regen to update blocks display
                //alternatively http://adndevblog.typepad.com/autocad/2012/05/redefining-a-block.html
                Application.DocumentManager.CurrentDocument.Editor.Regen();
            }
            else
            {
                acEditor.WriteMessage("No Plot Type currently open\n");
            }
        }

        [CommandMethod("PT_Finalise")]
        public static void Finalise()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {

                BlockTable bt = (BlockTable)tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);
                
                ObjectId newBlockId = bt[PlotType.CurrentOpen.PlotTypeName];
                BlockTableRecord btr = tr.GetObject(newBlockId, OpenMode.ForWrite) as BlockTableRecord;                
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

                foreach (ObjectId c in PlotType.CurrentOpen.AccessPointLocations.Collection)
                {
                    plotObjects.Add(c);
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
            OnCurrentOpenChanged?.Invoke();
        }

        private bool WSIntersect(Point3d point)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Transaction tr = acDoc.TransactionManager.TopTransaction;
            //Split the matching wall segment
            int deletionIndex = 0;
            bool found = false;
            for (int i = 0; i < Segments.Count; i++)
            {
                Line segment = tr.GetObject(Segments[i].PerimeterLine, OpenMode.ForRead) as Line;
                if (segment.GetGeCurve().IsOn(point))
                {
                    deletionIndex = i;
                    found = true;
                }
            }

            if (found)
            {
                var result = Segments[deletionIndex].Split(point);
                Segments.AddRange(result);
                Segments[deletionIndex].Erase();
                Segments.RemoveAt(deletionIndex);
            }

            return found;
        }

        public void Transfer(Database to, Database from)
        {
            CivilDocumentStore destinationStore = to.GetDocumentStore<CivilDocumentStore>();
            Transfer(to, from, destinationStore);
        }

        public ILibraryItem GetFrom(string Name, Database from)
        {
            CivilDocumentStore sourceStore = from.GetDocumentStore<CivilDocumentStore>();
            PlotType source = (from pt in sourceStore.PlotTypes where pt.PlotTypeName == Name select pt).First();
            return source;
        }

        private void Transfer(Database to, Database from, CivilDocumentStore destinationStore)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                ObjectIdCollection collection = new ObjectIdCollection();

                //Generate list of objects to transfer
                /*collection.Add(BackgroundBlockID);
                collection.Add(BasepointID);*/
                collection.Add(BlockID);

                /*foreach (ObjectId accessPointLocation in AccessPointLocations.Collection)
                {
                    collection.Add(accessPointLocation);
                }

                foreach (WallSegment wallSegment in Segments)
                {
                    collection.Add(wallSegment.PerimeterLine);
                }*/

                IdMapping acMapping = new IdMapping();

                to.WblockCloneObjects(collection, to.BlockTableId, acMapping, DuplicateRecordCloning.Ignore, false);

                PlotType destination = (PlotType) this.Clone();

                destination.BackgroundBlockID = TranslateMapping(BackgroundBlockID, acMapping);
                destination.BasepointID = TranslateMapping(BasepointID, acMapping);
                destination.BlockID = TranslateMapping(BlockID, acMapping);

                for (int i = 0; i < destination.AccessPointLocations.Count; i++)
                {
                    destination.AccessPointLocations[i] = TranslateMapping(AccessPointLocations[i], acMapping);
                }

                foreach (WallSegment wallSegment in destination.Segments)
                {
                    wallSegment.PerimeterLine = TranslateMapping(wallSegment.PerimeterLine, acMapping);
                }

                destinationStore.PlotTypes.Add(destination);
                destinationStore.Save();

                tr.Commit();
            }
        }

        ObjectId TranslateMapping(ObjectId sourceId, IdMapping acMapping)
        {
            foreach (IdPair pair in acMapping)
            {
                if (pair.Key == sourceId)
                {
                    return pair.Value;
                }
            }

            //throw new ArgumentException("No cloned object found");
            Logger.Log("No cloned object found", Logger.Severity.Error);
            return ObjectId.Null;
        }

        public object Clone()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xml = new XmlSerializer(typeof(PlotType));
                xml.Serialize(ms, this);
                ms.Position = 0;
                return xml.Deserialize(ms);
            }

            /*PlotType clone = new PlotType();
            clone.BlockID = BlockID;
            clone.BackgroundBlockID = BackgroundBlockID;
            clone.BasepointID = BasepointID;
            clone.PlotTypeName = PlotTypeName;
            clone.PerimeterLine = PerimeterLine;
            
            clone.AccessPointLocations = new PersistentObjectIdCollection();
            clone.AccessPointLocations.Pointers = new List<long>(AccessPointLocations.Pointers.ToArray());
            clone.AccessPoints = new List<AccessPoint>(AccessPoints.ToArray());
            clone.BasePoint = BasePoint;
            clone.Segments = new List<WallSegment>(Segments.ToArray());

            return clone;*/

        }
    }

    public struct AccessPoint
    {
        public Point3d Location;
        public double Offset;
        public string Guid;
    }
}
