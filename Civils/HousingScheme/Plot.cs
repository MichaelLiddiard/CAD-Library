using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using JPP.Core;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using CivSurface = Autodesk.Civil.DatabaseServices.Surface;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;
using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;

namespace JPP.Civils
{
    [Serializable]
    public class Plot
    {
        #region Public variables
        /// <summary>
        /// ID of the plot type this plot is generated from
        /// </summary>
        public string PlotTypeId { get; set; }

        /// <summary>
        /// Plot type that form ths basis for this plot
        /// </summary>
        [XmlIgnore]        
        public PlotType PlotType
        {
            get
            {
                if(_PlotType == null)
                {
                    //Relink the plot type by using the name if it is found to be missing
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

        #endregion

        public bool UpdateLevelsFromSurface;

        private PlotType _PlotType;

        public Point3d BasePoint { get; set; }

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

        public ObservableCollection<PlotLevel> Level;
        public ObservableCollection<PlotHatch> Hatches;

        public double FormationLevel { get; set; }

        public double FinishedFloorLevel { get; set; }

        public bool Locked { get; set; }

        public List<WallSegment> Segments;
        public List<WallJoint> Joints;

        [XmlIgnore]
        public List<WallJoint> PerimeterPath;

        Dictionary<Point3d, AccessPoint> AccessLookup;

        public PlotStatus Status { get; set; }
        public string StatusMessage;

        /// <summary>
        /// Create a new, empty plot. Constructor required for deserialization, not recommended for use
        /// </summary>
        public Plot()
        {
            WallSegments = new ObservableCollection<WallSegment>();
            Segments = new List<WallSegment>();
            Joints = new List<WallJoint>();
            Level = new ObservableCollection<PlotLevel>();
            Hatches = new ObservableCollection<PlotHatch>();
            AccessLookup = new Dictionary<Point3d, AccessPoint>();
            Status = PlotStatus.Error;
            StatusMessage = "Plot has remained as default.";
        }              

        /// <summary>
        /// Removes all existing hatching and regenerates it based on current  applied levels.
        /// </summary>
        public void GenerateHatching()
        {            
            //Remove all existing hatches
            foreach(PlotHatch ph in Hatches)
            {
                ph.Erase();
            }
            Hatches.Clear();

            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            //Set to only work for exposed brickwork
            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            //TODO: Optimise by cahcing 
            Polyline perimeter = new Polyline();
            //Build the perimeter line
            foreach (WallJoint wj in PerimeterPath)
            {
                perimeter.AddVertexAt(perimeter.NumberOfVertices, new Point2d(wj.Point.X, wj.Point.Y), 0, 0, 0);
            }
            perimeter.Closed = true;

            Polyline tankOffset = perimeter.GetOffsetCurves(-Properties.Settings.Default.P_Hatch_Offset)[0] as Polyline;
            Polyline exposedOffset = perimeter.GetOffsetCurves(Properties.Settings.Default.P_Hatch_Offset)[0] as Polyline;

            /*foreach(DBObject db in minus)
            {
                acBlkTblRec.AppendEntity(db as Entity);
                acTrans.AddNewlyCreatedDBObject(db, true);
            }
            foreach (DBObject db in plus)
            {
                acBlkTblRec.AppendEntity(db as Entity);
                acTrans.AddNewlyCreatedDBObject(db, true);
            }*/

            //Calculate first point not at ground level
            int startIndex = 0;
            for(int i = 0; i < PerimeterPath.Count; i++)
            {
                if(PerimeterPath[i].Courses() == 0)
                {
                    startIndex = i;
                    break;
                }
            }

            Point3dCollection hatchBoundaryPoints = new Point3dCollection();
            Point3dCollection hatchOffsetPoints = new Point3dCollection();
            bool hatchActive = false;

            //Iterate around perimeter
            for (int i = startIndex; i < PerimeterPath.Count + startIndex; i++)
            {
                int index;
                if(i >= PerimeterPath.Count)
                {
                    index = i - PerimeterPath.Count;
                }
                else
                {
                    index = i;
                }

                if(PerimeterPath[index].Courses() != 0)
                {
                    if(!hatchActive)
                    {                                                  
                        hatchActive = true;
                        hatchBoundaryPoints.Add(PerimeterPath[index - 1].Point);
                    }
                    hatchBoundaryPoints.Add(PerimeterPath[index].Point);
                    if (PerimeterPath[index].Courses() > 0)
                    {
                        hatchOffsetPoints.Add(exposedOffset.GetPoint3dAt(index));
                    }
                    else
                    {
                        hatchOffsetPoints.Add(tankOffset.GetPoint3dAt(index));
                    }
                } else //End potentially found
                {
                    if(hatchActive)
                    {
                        hatchActive = false;
                        hatchBoundaryPoints.Add(PerimeterPath[index].Point);
                        for(int y = hatchOffsetPoints.Count - 1; y >= 0; y--)
                        {
                            hatchBoundaryPoints.Add(hatchOffsetPoints[y]);
                        }

                        //Create hatch
                        PlotHatch ph = new PlotHatch();
                        ph.Generate(hatchBoundaryPoints);

                        hatchBoundaryPoints.Clear();
                        hatchOffsetPoints.Clear();

                        Hatches.Add(ph);
                    }
                }
            }

            /*BlockReference newBlockRef = (BlockReference)BlockRef.GetObject(OpenMode.ForWrite);
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
                    //Order levels by param along line to include doors in the correct place
                    var levelChanges = (from l in Level orderby l.Param ascending select l).ToList();

                    int startPoint = 0;
                    bool Tanking = false;

                    //Check if first point is ok by seeing if there is a 150mm difference or less
                    if (Math.Round(levelChanges[0].Level, 3) < -0.15 && !(levelChanges[0].Absolute == true && double.Parse(levelChanges[0].TextValue) < FinishedFloorLevel - 0.150))
                    {
                        //First point is not level, step backwards through the list untill point is found
                        int negCount = 0;
                        for (int i = levelChanges.Count - 1; i >= 0; i--)
                        {
                            if (Math.Round(levelChanges[i].Level, 3) < -0.15 && !(levelChanges[i].Absolute == true && double.Parse(levelChanges[i].TextValue) < FinishedFloorLevel - 0.150))
                            {
                                negCount--;
                            }
                            else
                            {               
                                //TODO: WOrk out why an if check? Thinks its so its doesnt go past the first ok pont. Change the outer if loop to a while??
                                if (startPoint == 0)
                                {
                                    startPoint = negCount;
                                }
                            }
                        }
                    }
                    
                    //Define point collections to hold the boundary coordinates
                    Point3dCollection hatchBoundaryPoints = new Point3dCollection();
                    Point3dCollection hatchOffsetPoints = new Point3dCollection();

                    // Create the offset polyline
                    //TODO: Add check here for very small lines not able to be offset
                    DBObjectCollection offsetOutlineObjects = acPline.GetOffsetCurves(Civils.Constants.TankingHatchOffest);
                    Polyline offsetOutline = offsetOutlineObjects[0] as Polyline;

                    for (int step = startPoint; step < levelChanges.Count; step++)
                    {
                        int i = 0; //Convert step to accessor
                        if (step < 0)
                        {
                            i = levelChanges.Count + startPoint;
                            
                        } else
                        {
                            i = step;
                        }

                        //if (Math.Round(levelChanges[i].Level, 3) != -0.15 && !(levelChanges[i].Absolute == true && double.Parse(levelChanges[i].TextValue) == FinishedFloorLevel - 0.150))
                        if (Math.Round(levelChanges[i].Level, 3) < -0.15 && !(levelChanges[i].Absolute == true && double.Parse(levelChanges[i].TextValue) < FinishedFloorLevel - 0.150))
                        {
                            if (!Tanking)
                            {
                                Tanking = true;
                                //Stop overflow if the first point is one
                                if (i == 0)
                                {
                                    hatchBoundaryPoints.Add(acPline.GetPointAtParameter(levelChanges[levelChanges.Count - 1].Param));
                                }
                                else
                                {
                                    hatchBoundaryPoints.Add(acPline.GetPointAtParameter(levelChanges[i - 1].Param));
                                }
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

                    //Loop finished without tank ending, therefore first point is end
                    if(Tanking)
                    {
                        Tanking = false;
                        hatchBoundaryPoints.Add(acPline.GetPointAtParameter(levelChanges[0].Param));

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
            }*/
        }

        /// <summary>
        /// Highlight all drawing elements
        /// </summary>
        public void Highlight()
        {
            //TODO: Implement
        }

        /// <summary>
        /// Unhighlight all drawing elements
        /// </summary>
        public void Unhighlight()
        {
            //TODO: Implement
        }

        public void GetFFLfromSurface()
        {
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            BlockReference newBlockRef = acTrans.GetObject(BlockRef, OpenMode.ForWrite) as BlockReference;//(BlockReference)BlockRef.GetObject(OpenMode.ForWrite);
            DBObjectCollection explodedBlock = new DBObjectCollection();
            newBlockRef.Explode(explodedBlock);

            ObjectIdCollection SurfaceIds = CivilApplication.ActiveDocument.GetSurfaceIds();
            CivSurface proposedGround = null;
            foreach (ObjectId surfaceId in SurfaceIds)
            {
                CivSurface oSurface = surfaceId.GetObject(OpenMode.ForRead) as CivSurface;
                if (oSurface.Name == Civils.Constants.ProposedGroundName)
                {
                    proposedGround = oSurface;
                }
            }

            double maxLevel = double.NegativeInfinity;
            foreach (WallJoint wj in Joints)
            {
                if (proposedGround != null)
                {
                    wj.ExternalLevel = proposedGround.FindElevationAtXY(wj.Point.X, wj.Point.Y);
                    if(wj.ExternalLevel > maxLevel)
                    {
                        maxLevel = wj.ExternalLevel;
                    }
                }                        
            }

            if(maxLevel == double.NegativeInfinity)
            {
                maxLevel = -0.15;
                this.Status = PlotStatus.Error;
                this.StatusMessage = "Plot not on proposed ground model or ground model not found";
            }

            this.FinishedFloorLevel = Math.Round(maxLevel * 1000) / 1000 + 0.15;
        }

        #region Plot commands        

        [CommandMethod("DeletePlot")]
        public static void DeletePlot()
        {

        }

        [CommandMethod("SetPlotFFL")]
        public static void SetPlotFFL()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (DocumentLock dl = acDoc.LockDocument())
            {
                var Plots = acDoc.GetDocumentStore<CivilDocumentStore>().Plots;

                foreach(Plot p in Plots)
                {
                    using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                    {
                        p.GetFFLfromSurface();
                        tr.Commit();
                    }
                }                
            }

            // Redraw the drawing
            Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
        }
        #endregion

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
                    }

                    c.StartPoint = c.StartPoint + newStart;
                    c.EndPoint = c.EndPoint + newEnd;

                    output.Add(c);
                }

                //tr.Commit();
            }

            return output;
        }

        /// <summary>
        /// Create the plot and establish all drawing objects. Only to be called when plot is first created or to be reset from plot types
        /// MUST be called from an active transaction
        /// <exception cref="ArgumentOutOfRangeException"> An ArugmentOutOfRageException is thrown when a matching wall segment in the plot type isnt found or is duplicated </exception>
        /// </summary>
        public void Generate()
        { 
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            BlockReference newBlockRef;

            //Link the block reference
            if (BlockRefPtr == 0)
            {
                BlockRef = Core.Utilities.InsertBlock(BasePoint, Rotation, PlotType.BlockID);
                newBlockRef = (BlockReference)BlockRef.GetObject(OpenMode.ForWrite);
            }
            else
            {
                //TODO: Add regeneration code.
                throw new NotImplementedException();
            }

            //Explode the blockref            
            DBObjectCollection explodedBlock = new DBObjectCollection();
            newBlockRef.Explode(explodedBlock);

            //TODO: Move releveant blocks here
            Main.LoadBlocks();

            AccessLookup = new Dictionary<Point3d, AccessPoint>();
            foreach (Entity entToAdd in explodedBlock)
            {
                //Pull the circles that id access points out
                if (entToAdd is Circle)
                {
                    Circle c = entToAdd as Circle;
                    if (c.Layer == Civils.Constants.JPP_HS_PlotPerimiter)
                    {
                        var rb = c.XData;
                        string target = "";
                        foreach (var tv in rb)
                        {
                            target = tv.Value as string;
                        }

                        Point3d? master = null;
                        AccessPoint? typeAP = null;

                        foreach (AccessPoint ap in this.PlotType.AccessPoints)
                        {
                            //TODO: Add code to match to PlotType WS by comparing start and end points
                            if (ap.Guid == target)
                            {
                                if (master == null)
                                {
                                    master = c.Center;
                                    typeAP = ap;
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException("Wall segment match already found", (System.Exception)null);
                                }
                            }
                        }

                        if (master == null)
                        {
                            throw new ArgumentOutOfRangeException("No matching wall segment found", (System.Exception)null);
                        }

                        AccessLookup.Add(master.Value, typeAP.Value);
                    }
                }
            }
            foreach (Entity entToAdd in explodedBlock)
            {
                
                //Identify the lines as these indicate wall segments
                //TODO: Add handling here for when a line is NOT a wall segmen
                if (entToAdd is Line)
                {
                    Line segment = entToAdd as Line;
                    string target = "";

                    //Get type id
                    var rb = segment.XData;
                    foreach(var tv in rb)
                    {
                        target = tv.Value as string;
                    }

                    WallSegment master = null;
                    WallSegment seg = new WallSegment();

                    foreach (WallSegment ptWS in this.PlotType.Segments)
                    {
                        //TODO: Add code to match to PlotType WS by comparing start and end points
                        if (ptWS.Guid == target)
                        {
                            if (master == null)
                            {
                                master = ptWS;
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException("Wall segment match already found", (System.Exception)null);
                            }
                        }
                    }

                    if (master == null)
                    {
                        throw new ArgumentOutOfRangeException("No matching wall segment found", (System.Exception)null);
                    }

                    //TODO: Check, think object is not part of database
                    seg.PerimeterLine = segment.ObjectId;
                    seg.StartPoint = segment.StartPoint;
                    seg.EndPoint = segment.EndPoint;
                    seg.Guid = master.Guid;
                    AddWallSegment(seg);
                }
            }

            //HANDLE EXTERNAL LEVELS

            //Traverse through wall segments to find external ones, starting with basepoint
            //Find basepoint
            WallJoint startNode = null;
            foreach(WallJoint wj in Joints)
            {
                if(wj.Point.IsEqualTo(BasePoint))
                {
                    startNode = wj;
                }
            }

            if(startNode == null)
            {
                throw new ArgumentOutOfRangeException("Basepoint does not lie on wall joint", (System.Exception)null);
            }                       

            //Recursively traverse through the segments to find the external ones
            WallSegment startSegment = startNode.North;
            WallJoint nextNode;
            if (startSegment.EndPoint == startNode.Point)
            {
                nextNode = startSegment.StartJoint;
            }
            else
            {
                nextNode = startSegment.EndJoint;
            }

            PerimeterPath = new List<WallJoint>();
            PerimeterPath.Add(startNode);
            TraverseExternalSegment(startSegment, nextNode, startNode);            

            //GENERATE SUB ELEMENTS

            foreach(WallJoint wj in PerimeterPath)
            {
                wj.Generate(Rotation);
            }

            //Check to see if FFL needs to be set
            if (UpdateLevelsFromSurface)
            {                
                GetFFLfromSurface();                
            }

            Update();

            Status = PlotStatus.ForApproval;
            StatusMessage = "Plot generated successfully";

            /*Polyline acPline = entToAdd as Polyline;
            foreach (AccessPoint ap in PlotType.AccessPoints)
            {
                PlotLevel pl = new PlotLevel(false, ap.Offset, this, ap.Parameter);
                pl.Generate(acPline.GetPointAtParameter(ap.Parameter));
                Level.Add(pl);
            }

            //Iterate over all the corners and add to the drawing
            int vn = acPline.NumberOfVertices;
            for (int i = 0; i < vn; i++)
            {                        
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
                //Find the centre of the plot outline as an estimated point of insertion
                Solid3d Solid = new Solid3d();
                DBObjectCollection coll = new DBObjectCollection
                {
                    acPline
                };
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

            GenerateHatching();*/


        }

        /// <summary>
        /// Update all drawing elements
        /// </summary>
        public void Update()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                foreach(WallJoint wj in Joints)
                {
                    wj.Update(this.FinishedFloorLevel);
                }

                GenerateHatching();
                /*//Update all plot level annotations
                foreach (PlotLevel pl in Level)
                {
                    pl.Update();
                }

                //Update FFL annotation
                MText text = tr.GetObject(FFLLabel, OpenMode.ForWrite) as MText;
                text.Contents = FinishedFloorLevelText;

                //Generate all hatching for tanking/retaining etc
                this.GenerateHatching();*/

                //Commit the changes
                tr.Commit();
            }
        }

        private void TraverseExternalSegment(WallSegment currentSegment, WallJoint currentNode, WallJoint startNode)
        {
            currentSegment.External = true;
            PerimeterPath.Add(currentNode);
            //Get next
            WallSegment nextSegment = currentNode.NextClockwise(currentSegment);
            WallJoint nextJoint;
            if(nextSegment.StartPoint.IsEqualTo(currentNode.Point))
            {
                nextJoint = nextSegment.EndJoint;
            } else
            {
                nextJoint = nextSegment.StartJoint;
            }
            
            if(nextJoint.Point.IsEqualTo(startNode.Point))
            {
                //Back to start
                nextSegment.External = true;                
            } else
            {
                TraverseExternalSegment(nextSegment, nextJoint, startNode);
            }
            
        }

        /// <summary>
        /// Helper method for adding wall segments to ensure everything gets linked correctly.
        /// </summary>
        private void AddWallSegment(WallSegment newSegment)
        {
            Segments.Add(newSegment);

            bool startFound = false;
            bool endFound = false;

            foreach(WallJoint wj in Joints)
            {
                if(wj.Point.IsEqualTo(newSegment.StartPoint))
                {
                    startFound = true;
                    newSegment.StartJoint = wj;
                    wj.AddWallSegment(newSegment);
                }
                if (wj.Point.IsEqualTo(newSegment.EndPoint))
                {
                    endFound = true;
                    newSegment.EndJoint = wj;
                    wj.AddWallSegment(newSegment);
                }
            }

            if(!startFound)
            {
                WallJoint newWj = new WallJoint();
                newWj.Point = newSegment.StartPoint;
                newSegment.StartJoint = newWj;
                newWj.AddWallSegment(newSegment);

                if(AccessLookup.ContainsKey(newWj.Point))
                {
                    newWj.RelativeOffset = AccessLookup[newWj.Point].Offset;
                }

                Joints.Add(newWj);
            }

            if (!endFound)
            {
                WallJoint newWj = new WallJoint();
                newWj.Point = newSegment.EndPoint;
                newSegment.EndJoint = newWj;
                newWj.AddWallSegment(newSegment);

                if (AccessLookup.ContainsKey(newWj.Point))
                {
                    newWj.RelativeOffset = AccessLookup[newWj.Point].Offset;
                }

                Joints.Add(newWj);
            }

            newSegment.Generate();
        }
    }

    public enum PlotStatus
    {
        Approved,
        ForApproval,
        Warning,
        Error
    }
}
