using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JPP.Core;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: CommandClass(typeof(JPP.Civils.Plot))]

namespace JPP.Civils
{
    [Serializable]
    public class Plot
    {        
        public string PlotTypeId { get; set; }

        [XmlIgnore]
        public PlotType PlotType
        {
            get
            {
                if(_PlotType == null)
                {
                    var pt = from p in Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().PlotTypes where p.PlotTypeName == PlotTypeId select p;
                    _PlotType = pt.First();
                }

                return _PlotType;
            }
            set
            {
                PlotTypeId = value.PlotTypeName;
                _PlotType = value;
            }
        }
        private PlotType _PlotType;

        Point3d BasePoint { get; set; }

        public double Rotation { get; set; }

        public ObservableCollection<WallSegment> WallSegments { get; set; }

        public string PlotName { get; set; }

        public long BlockRefPtr { get; set; }

        public string FinishedFloorLevelText
        {
            get
            {
                return "FFL: " + FinishedFloorLevel;
            }
        }

        [XmlIgnore]
        public ObjectId BlockRef
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(BlockRefPtr), 0);
            }
            set
            {
                BlockRefPtr = value.Handle.Value;
            }
        }

        public long FFLLabelPtr { get; set; }

        [XmlIgnore]
        public ObjectId FFLLabel
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(FFLLabelPtr), 0);
            }
            set
            {
                FFLLabelPtr = value.Handle.Value;
            }
        }

        /*public List<long> LevelPtr { get; set; }

        [XmlIgnore]
        public ObservableCollection<ObjectId> Level
        {
            get
            {
                if(_Level == null)
                {
                    _Level = new ObservableCollection<ObjectId>();
                    _Level.CollectionChanged += _Level_CollectionChanged;
                    foreach (long l in LevelPtr)
                    {                        
                        Document acDoc = Application.DocumentManager.MdiActiveDocument;
                        Database acCurDb = acDoc.Database;
                        _Level.Add(acCurDb.GetObjectId(false, new Handle(l), 0));
                    }
                }

                return _Level;
                
            }
            set
            {
                _Level = value;
                _Level.CollectionChanged += _Level_CollectionChanged;
            }
        }

        private void _Level_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach(ObjectId oi in e.NewItems)
                {
                    LevelPtr.Add(oi.Handle.Value);
                }
            }
        }

        [XmlIgnore]
        private ObservableCollection<ObjectId> _Level;*/

        public ObservableCollection<PlotLevel> Level;
        public ObservableCollection<PlotHatch> Hatches;

        public Plot()
        {
            WallSegments = new ObservableCollection<WallSegment>();
            Level = new ObservableCollection<PlotLevel>();
            Hatches = new ObservableCollection<PlotHatch>();
            //LevelPtr = new List<long>();
        }       

        public double FormationLevel { get; set; }

        public double FinishedFloorLevel { get; set; }

        public bool Locked { get; set; }

        public void Update()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())//acCurDb.TransactionManager.StartTransaction())
            {
                foreach (PlotLevel pl in Level)
                {
                    pl.Update();
                }

                MText text = tr.GetObject(FFLLabel, OpenMode.ForWrite) as MText;
                text.Contents = FinishedFloorLevelText;

                /*foreach (WallSegment ws in WallSegments)
                {
                    ws.Update();
                }

                List<Curve> foundations = new List<Curve>();
                foreach (WallSegment ws in WallSegments)
                {
                    foundations.Add(tr.GetObject(ws.NegativeFoundationId, OpenMode.ForWrite) as Curve);
                    foundations.Add(tr.GetObject(ws.PositiveFoundationId, OpenMode.ForWrite) as Curve);
                }

                TrimFoundation(foundations);*/

                

                tr.Commit();
            }
        }

        public static List<Curve> TrimFoundation(List<Curve> allLines)
        {
            List<Curve> output = new List<Curve>();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            DBObjectCollection remove = new DBObjectCollection();
            List<Curve> perimeters = new List<Curve>();

            using (Transaction tr = acCurDb.TransactionManager.TopTransaction)//acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (Curve c in allLines)
                {
                    Point3dCollection points = new Point3dCollection();

                    foreach (Curve target in allLines)
                    {
                        Point3dCollection pointsAppend = new Point3dCollection();
                        c.IntersectWith(target, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d p3d in pointsAppend)
                        {
                            points.Add(p3d);
                        }
                    }

                    if (points.Count == 2)
                    {
                        List<double> splitPoints = new List<double>();
                        foreach (Point3d p3d in points)
                        {
                            //splitPoints.Add(c.GetParameterAtPoint(p3d));
                            splitPoints.Add(c.GetParameterAtPoint(p3d));
                        }
                        splitPoints.Sort();
                        DoubleCollection acadSplitPoints = new DoubleCollection(splitPoints.ToArray());
                        DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                        acBlkTblRec.AppendEntity(remnant[1] as Entity);
                        tr.AddNewlyCreatedDBObject(remnant[1] as Entity, true);
                        output.Add(remnant[1] as Curve);
                        //c.HandOverTo(remnant[1] as Entity, true, true);
                        c.SwapIdWith(remnant[1].ObjectId, true, true);
                        remove.Add(c);
                    }

                    if (points.Count == 1)
                    {
                        List<double> splitPoints = new List<double>();
                        foreach (Point3d p3d in points)
                        {
                            //splitPoints.Add(c.GetParameterAtPoint(p3d));
                            splitPoints.Add(c.GetParameterAtPoint(p3d));
                        }
                        splitPoints.Sort();
                        DoubleCollection acadSplitPoints = new DoubleCollection(splitPoints.ToArray());

                        foreach (double d in acadSplitPoints)
                        {
                            double percent = c.GetDistanceAtParameter(d) / c.GetDistanceAtParameter(c.EndParam);
                            if (percent < 0.5 && percent > 0)
                            {
                                DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                                acBlkTblRec.AppendEntity(remnant[1] as Entity);
                                tr.AddNewlyCreatedDBObject(remnant[1] as Entity, true);
                                perimeters.Add(remnant[1] as Curve);
                                output.Add(remnant[1] as Curve);
                                //c.HandOverTo(remnant[1] as Entity, true, true);
                                c.SwapIdWith(remnant[1].ObjectId, true, true);
                                remove.Add(c);
                            }
                            if (percent < 1 && percent >= 0.5)
                            {
                                DBObjectCollection remnant = c.GetSplitCurves(acadSplitPoints);
                                acBlkTblRec.AppendEntity(remnant[0] as Entity);
                                tr.AddNewlyCreatedDBObject(remnant[0] as Entity, true);
                                perimeters.Add(remnant[0] as Curve);
                                output.Add(remnant[0] as Curve);
                                //c.HandOverTo(remnant[0] as Entity, true, true);
                                c.SwapIdWith(remnant[0].ObjectId, true, true);
                                remove.Add(c);
                            }
                        }
                    }
                    if (points.Count == 0)
                    {
                        perimeters.Add(c);
                    }
                }

                foreach (DBObject obj in remove)
                {
                    obj.Erase();
                    //obj.Dispose();
                }

                List<Curve> complete = output;
                complete.AddRange(perimeters);

                //Iterate over the perimeter lines
                foreach (Curve c in perimeters)
                {
                    Point3dCollection points = new Point3dCollection();

                    foreach (Curve target in perimeters)
                    {
                        Point3dCollection pointsAppend = new Point3dCollection();
                        c.IntersectWith(target, Intersect.ExtendBoth, points, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d p3d in pointsAppend)
                        {
                            points.Add(p3d);
                        }
                    }

                    Vector3d newEnd = new Vector3d();
                    Vector3d newStart = new Vector3d();
                    double endDelta = Double.PositiveInfinity;
                    double startDelta = Double.PositiveInfinity;


                    foreach (Point3d p3d in points)
                    {
                        Line start = new Line(c.StartPoint, p3d);
                        Line end = new Line(c.EndPoint, p3d);

                        //Can only affect either end or start based on closest
                        if (start.Delta.LengthSqrd < end.Delta.LengthSqrd)
                        {
                            if (start.Delta.LengthSqrd < startDelta)
                            {
                                startDelta = start.Delta.LengthSqrd;
                                newStart = start.Delta;
                            }
                        }
                        else
                        {
                            if (end.Delta.LengthSqrd < endDelta)
                            {
                                endDelta = end.Delta.LengthSqrd;
                                newEnd = end.Delta;
                            }
                        }
                    }

                    //Check to see if lines meet at same point
                    foreach (Curve target in complete)
                    {
                        //Avoid self
                        if (target.ObjectId != c.ObjectId)
                        {

                            if (target.EndPoint == c.EndPoint || target.StartPoint == c.EndPoint)
                            {
                                newEnd = new Vector3d();
                            }
                            if (target.EndPoint == c.StartPoint || target.StartPoint == c.StartPoint)
                            {
                                newStart = new Vector3d();
                            }
                        }
                        else
                        {
                            int i = 0;
                        }
                    }

                    c.StartPoint = c.StartPoint + newStart;
                    c.EndPoint = c.EndPoint + newEnd;

                    output.Add(c);
                }

                //tr.Commit();
            }

            return output;
        }

        public void Generate()
        {
            
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            BlockReference newBlockRef;

            if (BlockRefPtr == 0)
            {
                /*//Create new block reference
                // Add a block reference to model space. 
                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                newBlockRef = new BlockReference(BasePoint, PlotType.BlockID);                
                BlockRef = acBlkTblRec.AppendEntity(newBlockRef);
                acTrans.AddNewlyCreatedDBObject(newBlockRef, true);*/
                BlockRef = Core.Utilities.InsertBlock(BasePoint, Rotation, PlotType.BlockID);
                newBlockRef = (BlockReference) BlockRef.GetObject(OpenMode.ForWrite);

            }
            else
            {
                throw new NotImplementedException();
            }

            //Explode the blockref            
            DBObjectCollection explodedBlock = new DBObjectCollection();
            newBlockRef.Explode(explodedBlock);

            Main.LoadBlocks();

            foreach (Entity entToAdd in explodedBlock)
            {                
                if (entToAdd is Polyline)
                {
                    Polyline acPline = entToAdd as Polyline;
                    foreach (AccessPoint ap in PlotType.AccessPoints)
                    {
                        PlotLevel pl = new PlotLevel(false, ap.Offset, this, ap.Parameter);
                        pl.Generate(acPline.GetPointAtParameter(ap.Parameter));
                        Level.Add(pl);
                    }

                    int vn = acPline.NumberOfVertices;
                    for (int i = 0; i < vn; i++)
                    {
                        // Could also get the 3D point here
                        Point3d pt = acPline.GetPoint3dAt(i);
                        PlotLevel pl = new PlotLevel(false, -0.15, this, acPline.GetParameterAtPoint(pt));
                        pl.Generate(pt);
                        Level.Add(pl);
                    }

                    // Open the Block table for read
                    BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    //Ad the FFL Label
                    // Create a multiline text object
                    using (MText acMText = new MText())
                    {
                        Solid3d Solid = new Solid3d();
                        DBObjectCollection coll = new DBObjectCollection();
                        coll.Add(acPline);
                        Solid.Extrude(((Region)Region.CreateFromCurves(coll)[0]), 1, 0);
                        Point3d centroid = new Point3d(Solid.MassProperties.Centroid.X, Solid.MassProperties.Centroid.Y, 0);
                        Solid.Dispose();

                        acMText.Location = centroid;
                        acMText.Contents = FinishedFloorLevelText;
                        acMText.Rotation = Rotation;
                        acMText.Height = 7;
                        acMText.Attachment = AttachmentPoint.MiddleCenter;

                        FFLLabel = acBlkTblRec.AppendEntity(acMText);
                        acTrans.AddNewlyCreatedDBObject(acMText, true);
                    }

                    GenerateHatching();
                }
            }          

            /*
            // Open the Block table for read
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            ObjectId blkRecId = ObjectId.Null;

            if (!acBlkTbl.Has("FormationTag"))
            {
                Core.Utilities.LoadBlocks();
            }

            foreach (WallSegment ws in WallSegments)
            {
                ws.Generate();
            }*/
        }

        public void GenerateHatching()
        {            
            foreach(PlotHatch ph in Hatches)
            {
                ph.Erase();
            }
            Hatches.Clear();

            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            BlockReference newBlockRef = (BlockReference)BlockRef.GetObject(OpenMode.ForWrite);
            DBObjectCollection explodedBlock = new DBObjectCollection();
            newBlockRef.Explode(explodedBlock);

            foreach (Entity entToAdd in explodedBlock)
            {
                if (entToAdd is Polyline)
                {
                    Polyline acPline = entToAdd as Polyline;

                    // Open the Block table for read
                    BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    //Add tanking and exposed brickwork
                    var levelChanges = (from l in Level orderby l.Param ascending select l).ToList();

                    //Check if first point is ok
                    if (Math.Round(levelChanges[0].Level, 3) != -0.15 && !(levelChanges[0].Absolute == true && double.Parse(levelChanges[0].TextValue) == FinishedFloorLevel - 150))
                    {
                        //TODO: Need to handle first point not being at level
                        throw new NotImplementedException();
                    }

                    bool Tanking = false;
                    Point3dCollection hatchBoundaryPoints = new Point3dCollection();
                    Point3dCollection hatchOffsetPoints = new Point3dCollection();

                    // Create the offset polyline
                    //TODO: Add check here for very small lines not able to be offset
                    DBObjectCollection offsetOutlineObjects = acPline.GetOffsetCurves(0.500);
                    Polyline offsetOutline = offsetOutlineObjects[0] as Polyline;

                    for (int i = 0; i < levelChanges.Count; i++)
                    {
                        if (Math.Round(levelChanges[i].Level, 3) != -0.15 && !(levelChanges[i].Absolute == true && double.Parse(levelChanges[0].TextValue) == FinishedFloorLevel - 150))
                        {
                            if (!Tanking)
                            {
                                Tanking = true;
                                hatchBoundaryPoints.Add(acPline.GetPointAtParameter(levelChanges[i - 1].Param));
                            }
                            hatchBoundaryPoints.Add(acPline.GetPointAtParameter(levelChanges[i].Param));
                            hatchOffsetPoints.Add(offsetOutline.GetPointAtParameter(levelChanges[i].Param));
                        }
                        else
                        {
                            if (Tanking)
                            {
                                //end point found
                                Tanking = false;
                                hatchBoundaryPoints.Add(acPline.GetPointAtParameter(levelChanges[i].Param));

                                //Create hatch object
                                //Traverse outline backwards to pick up points
                                for (int j = hatchOffsetPoints.Count - 1; j >= 0; j--)
                                {
                                    hatchBoundaryPoints.Add(hatchOffsetPoints[j]);
                                }

                                // Lose this line in the real command
                                bool success = JPPCommandsInitialisation.setJPPLayers();

                                PlotHatch ph = new PlotHatch();
                                ph.Generate(hatchBoundaryPoints);
                                Hatches.Add(ph);

                                hatchBoundaryPoints.Clear();
                                hatchOffsetPoints.Clear();
                            }
                        }
                    }
                }
            }
        }

        public void Lock()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                DBDictionary gd = (DBDictionary)tr.GetObject(acCurDb.GroupDictionaryId, OpenMode.ForWrite);
                Group group = new Group("Plot group", true);
                gd.SetAt(PlotName, group);
                tr.AddNewlyCreatedDBObject(group, true);

                group.InsertAt(0, BlockRef);
                foreach(PlotLevel pl in Level)
                {
                    pl.Lock(group);                    
                }

                tr.Commit();
            }
        }

        public void Unlock()
        {


        }

        public void Rebuild()
        {
            foreach(WallSegment ws in WallSegments)
            {
                ws.Parent = this;
            }
            foreach(PlotLevel pl in Level)
            {
                pl.Parent = this;
            }
        }

        public void Regen()
        {

        }

        public void Highlight()
        {

        }

        public void Unhighlight()
        {

        }

        [CommandMethod("NewPlot")]
        public static void NewPlot()
        {
            JPPCommands.JPPCommandsInitialisation.JPPCommandsInitialise();

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            /*PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter plot type name: ");

            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);*/
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "Enter plot type: ";

            foreach(PlotType pt in acDoc.GetDocumentStore<CivilDocumentStore>().PlotTypes)
            {
                pKeyOpts.Keywords.Add(pt.PlotTypeName);
            }            
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            string plotTypeId = pKeyRes.StringResult;

            PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter plot name: ");
            pStrOptsPlot.AllowSpaces = true;
            PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);
            string plotId = pStrResPlot.StringResult;

            PromptDoubleResult promptFFLDouble = acDoc.Editor.GetDouble("\nEnter the FFL: ");

            Plot p = new Plot();
            p.PlotName = plotId;
            p.PlotTypeId = plotTypeId;
            p.FinishedFloorLevel = promptFFLDouble.Value;
            
            PromptPointOptions pPtOpts = new PromptPointOptions("\nEnter base point of the plot: ");
            PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);
            p.BasePoint = pPtRes.Value;

            PromptPointOptions pAnglePtOpts = new PromptPointOptions("\nSelect point on base line: ");
            PromptPointResult pAnglePtRes = acDoc.Editor.GetPoint(pAnglePtOpts);
            Point3d p3d = pAnglePtRes.Value;
            double x, y;
            x = p3d.X - p.BasePoint.X;
            y = p3d.Y - p.BasePoint.Y;
            p.Rotation = Math.Atan(y / x);

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                p.Generate();
                tr.Commit();
            }

            p.Lock();

            acDoc.GetDocumentStore<CivilDocumentStore>().Plots.Add(p);
        }

        [CommandMethod("DeletePlot")]
        public static void DeletePlot()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter plot name: ");

            pStrOpts.AllowSpaces = true;
            PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);
            string plotName = pStrRes.StringResult;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "Enter plot type: ";

            foreach (PlotType pt in acDoc.GetDocumentStore<CivilDocumentStore>().PlotTypes)
            {
                pKeyOpts.Keywords.Add(pt.PlotTypeName);
            }
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            string plotTypeId = pKeyRes.StringResult;

            PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter plot name: ");
            pStrOptsPlot.AllowSpaces = true;
            PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);
            string plotId = pStrResPlot.StringResult;

            PromptDoubleResult promptFFLDouble = acDoc.Editor.GetDouble("\nEnter the FFL: ");

            Plot p = new Plot();
            p.PlotName = plotId;
            p.PlotTypeId = plotTypeId;
            p.FinishedFloorLevel = promptFFLDouble.Value;

            PromptPointOptions pPtOpts = new PromptPointOptions("\nEnter base point of the plot: ");
            PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);
            p.BasePoint = pPtRes.Value;

            PromptPointOptions pAnglePtOpts = new PromptPointOptions("\nSelect point on base line: ");
            PromptPointResult pAnglePtRes = acDoc.Editor.GetPoint(pAnglePtOpts);
            Point3d p3d = pAnglePtRes.Value;
            double x, y;
            x = p3d.X - p.BasePoint.X;
            y = p3d.Y - p.BasePoint.Y;
            p.Rotation = Math.Atan(y / x);

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                p.Generate();
                tr.Commit();
            }

            p.Lock();

            acDoc.GetDocumentStore<CivilDocumentStore>().Plots.Add(p);
        }
    }
}
