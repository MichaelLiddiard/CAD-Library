using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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
    public class PlotType
    {
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

        public Point3d BasePoint;

        public PlotType()
        {
            AccessPoints = new List<AccessPoint>();
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
                foreach(ObjectId e in btr)
                {
                    Entity temp = tr.GetObject(e, OpenMode.ForWrite) as Entity;
                    temp.Erase();
                }
                
            } else
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

        [CommandMethod("CreatePlotType")]
        public static void CreatePlotType()
        {
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
            }
        }
    }    

    public struct AccessPoint
    {
        public double Parameter;
        public double Offset;
    }
}
