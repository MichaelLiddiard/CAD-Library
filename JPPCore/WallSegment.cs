using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPP.Core
{
    public class WallSegment
    {
        [XmlIgnore]
        public Plot Parent;

        public string Name;

        public long ObjectIdPtr;

        [XmlIgnore]
        public ObjectId ObjectId
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(ObjectIdPtr), 0);               
            }
            set
            {
                ObjectIdPtr = value.Handle.Value;
            }
        }

        /// <summary>
        /// Load in kN/m
        /// </summary>
        public double Load { get; set; }

        /// <summary>
        /// Width of wall atop foundation
        /// </summary>
        public double WallWidth { get; set; }

        public double FoundationWidth { get; set; }

        public long FormationTagPtr;

        [XmlIgnore]
        public ObjectId FormationTagId
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(FormationTagPtr), 0);
            }
            set
            {
                FormationTagPtr = value.Handle.Value;
            }
        }

        public long PositiveFoundationPtr;

        [XmlIgnore]
        public ObjectId PositiveFoundationId
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(PositiveFoundationPtr), 0);
            }
            set
            {
                PositiveFoundationPtr = value.Handle.Value;
            }
        }

        public long NegativeFoundationPtr;

        [XmlIgnore]
        public ObjectId NegativeFoundationId
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(NegativeFoundationPtr), 0);
            }
            set
            {
                NegativeFoundationPtr = value.Handle.Value;
            }
        }

        public WallSegment()
        {

        }

        public WallSegment(string Name, Plot parent, ObjectId centreline)
        {
            this.Parent = parent;
            this.Name = Name;
            ObjectId = centreline;
            FoundationWidth = DocumentStore.Current.DefaultWidth;

            //Establish links
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                DBObject dbObj = tr.GetObject(centreline, OpenMode.ForRead);

                ObjectId extId = dbObj.ExtensionDictionary;

                if (extId == ObjectId.Null)
                {
                    dbObj.UpgradeOpen();
                    dbObj.CreateExtensionDictionary();
                    extId = dbObj.ExtensionDictionary;
                }

                //now we will have extId...
                DBDictionary dbExt = (DBDictionary)tr.GetObject(extId, OpenMode.ForWrite);

                Xrecord xRec = new Xrecord();
                ResultBuffer rb = new ResultBuffer();
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, Parent.PlotName));
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, this.Name));

                //set the data
                xRec.Data = rb;

                dbExt.SetAt("JPP_Plot", xRec);
                tr.AddNewlyCreatedDBObject(xRec, true);

                tr.Commit();
            }
        }

        public void Update()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            Transaction tr = acCurDb.TransactionManager.TopTransaction;

            //Centre line
            Curve c = tr.GetObject(ObjectId, OpenMode.ForRead) as Curve;

            //Delete existing
            NegativeFoundationId.GetObject(OpenMode.ForWrite, true).Erase();
            PositiveFoundationId.GetObject(OpenMode.ForWrite, true).Erase();

            //Offset it
            LayerTable acLyrTbl = tr.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;
            ObjectId current = acCurDb.Clayer;
            acCurDb.Clayer = acLyrTbl[Utilities.FoundationLayer];

            // Open the Block table for read
            BlockTable acBlkTbl = tr.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Open the Block table record Model space for write
            BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            DBObjectCollection offsets = c.GetOffsetCurves(FoundationWidth / 2);
            DBObjectCollection offsets2 = c.GetOffsetCurves(-(FoundationWidth / 2));
            foreach (Entity e in offsets)
            {
                acBlkTblRec.AppendEntity(e);
                tr.AddNewlyCreatedDBObject(e, true);
                e.LayerId = acCurDb.Clayer;
                PositiveFoundationId = e.ObjectId;
            }
            foreach (Entity e in offsets2)
            {
                acBlkTblRec.AppendEntity(e);
                tr.AddNewlyCreatedDBObject(e, true);
                e.LayerId = acCurDb.Clayer;
                NegativeFoundationId = e.ObjectId;
            }


            acCurDb.Clayer = current;

            //Update formation tag
            if (FormationTagId != null)
            {
                BlockReference acBlkRef = tr.GetObject(FormationTagId, OpenMode.ForRead) as BlockReference;

                //Set value
                AttributeCollection attCol = acBlkRef.AttributeCollection;
                foreach (ObjectId attId in attCol)
                {
                    AttributeReference att = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (att.Tag == "LEVEL")
                    {
                        att.UpgradeOpen();
                        att.TextString = Parent.FormationLevel.ToString("F3");
                    }
                }
            }
        }

        public void Generate()
        {
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            ObjectContextCollection occ = acCurDb.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");

            Transaction acTrans = acCurDb.TransactionManager.TopTransaction;

            //Centre line
            Curve c = acTrans.GetObject(ObjectId, OpenMode.ForRead) as Curve;

            //Offset it
            LayerTable acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;
            ObjectId current = acCurDb.Clayer;
            acCurDb.Clayer = acLyrTbl[Utilities.FoundationLayer];

            // Open the Block table for read
            BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            // Open the Block table record Model space for write
            BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                DBObjectCollection offsets = c.GetOffsetCurves(FoundationWidth / 2);
                DBObjectCollection offsets2 = c.GetOffsetCurves(-(FoundationWidth / 2));
                foreach (Entity e in offsets)
                {
                    acBlkTblRec.AppendEntity(e);
                acTrans.AddNewlyCreatedDBObject(e, true);
                    e.LayerId = acCurDb.Clayer;
                PositiveFoundationId = e.ObjectId;
                }
                foreach (Entity e in offsets2)
                {
                    acBlkTblRec.AppendEntity(e);
                acTrans.AddNewlyCreatedDBObject(e, true);
                    e.LayerId = acCurDb.Clayer;
                NegativeFoundationId = e.ObjectId;
                }
            

            acCurDb.Clayer = current;

            //Tag it
            acCurDb.Clayer = acLyrTbl[Utilities.FoundationTextLayer];

            //Add foundation tag
            Matrix3d curUCSMatrix = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
            CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;            

            //Get lable point
            Point3d labelPoint3d = c.GetPointAtDist(c.GetDistanceAtParameter(c.EndParam) / 2);
            Point3d labelPoint = new Point3d(labelPoint3d.X, labelPoint3d.Y, 0);

            BlockTableRecord blockDef = acBlkTbl["FormationTag"].GetObject(OpenMode.ForRead) as BlockTableRecord;

            // Insert the block into the current space
            using (BlockReference acBlkRef = new BlockReference(labelPoint, acBlkTbl["FormationTag"]))
            {
                //Calculate Line Angle
                double y = c.EndPoint.Y - c.StartPoint.Y;
                double x = c.EndPoint.X - c.StartPoint.X;
                double angle = Math.Atan(Math.Abs(y) / Math.Abs(x));
                if (angle >= Math.PI / 4)
                {
                    acBlkRef.TransformBy(Matrix3d.Rotation(0, curUCS.Zaxis, labelPoint));
                }
                else
                {
                    acBlkRef.TransformBy(Matrix3d.Rotation(Math.PI / 2, curUCS.Zaxis, labelPoint));
                }
                acBlkRef.AddContext(occ.GetContext("10:1"));

                acBlkTblRec = acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                acBlkTblRec.AppendEntity(acBlkRef);
                acTrans.AddNewlyCreatedDBObject(acBlkRef, true);

                // AttributeDefinitions
                foreach (ObjectId id in blockDef)
                {
                    DBObject obj = id.GetObject(OpenMode.ForRead);
                    AttributeDefinition attDef = obj as AttributeDefinition;
                    if ((attDef != null) && (!attDef.Constant))
                    {
                        //This is a non-constant AttributeDefinition
                        //Create a new AttributeReference
                        using (AttributeReference attRef = new AttributeReference())
                        {
                            attRef.SetAttributeFromBlock(attDef, acBlkRef.BlockTransform);
                            attRef.TextString = Parent.FormationLevel.ToString("F3");
                            //Add the AttributeReference to the BlockReference
                            acBlkRef.AttributeCollection.AppendAttribute(attRef);
                            acTrans.AddNewlyCreatedDBObject(attRef, true);
                        }
                    }
                }

                FormationTagId = acBlkRef.ObjectId;
            }

            acCurDb.Clayer = current;
        }
    }
}
