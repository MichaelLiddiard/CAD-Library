using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    [Serializable]
    public class WallSegment
    {
        public Plot Parent;

        public string Name;

        public IntPtr ObjectIdPtr;

        public ObjectId ObjectId
        {
            get
            {
                return new ObjectId(ObjectIdPtr);
            }
            set
            {
                ObjectIdPtr = value.OldIdPtr;
            }
        }

        /// <summary>
        /// Load in kN/m
        /// </summary>
        public double Load;

        /// <summary>
        /// Width of wall atop foundation
        /// </summary>
        public double WallWidth;

        public IntPtr FormationTagPtr;

        public ObjectId FormationTagId
        {
            get
            {
                return new ObjectId(FormationTagPtr);
            }
            set
            {
                FormationTagPtr = value.OldIdPtr;
            }
        }

        public WallSegment(string Name, Plot parent, ObjectId centreline)
        {
            this.Parent = parent;
            this.Name = Name;
            ObjectId = centreline;

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

            BlockReference acBlkRef = tr.GetObject(FormationTagId, OpenMode.ForRead) as BlockReference;


                //Set value
                AttributeCollection attCol = acBlkRef.AttributeCollection;
            foreach (ObjectId attId in attCol)
            {
                AttributeReference att = tr.GetObject(attId, OpenMode.ForRead, false) as AttributeReference;
                if (att.Tag == "LEVEL")
                {
                    att.UpgradeOpen();
                    att.TextString = Parent.FormationLevel.ToString();
                }
            }
        }
    }
}
