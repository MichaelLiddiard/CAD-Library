﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPP.Civils
{
    public class PlotLevel
    {
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

        public long TextPtr;

        [XmlIgnore]
        public ObjectId Text
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(TextPtr), 0);
            }
            set
            {
                TextPtr = value.Handle.Value;
            }
        }

        public double Level { get; set; }

        public bool Absolute { get; set; }

        [XmlIgnore]
        public Plot Parent { get; set; }

        public string TextValue
        {
            get
            {
                if (Absolute)
                {
                    return Level.ToString("F3");
                }
                else
                {
                    return (Parent.FinishedFloorLevel - Level).ToString("F3");
                }
            }
        }

        public PlotLevel()
        {

        }

        public PlotLevel(bool absolute, double Level, Plot Parent)
        {
            this.Absolute = absolute;
            this.Level = Level;
            this.Parent = Parent;
        }

        public void Generate(Point3d location)
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction trans = acCurDb.TransactionManager.TopTransaction;

            //string contents = (Parent.FinishedFloorLevel + ap.Offset).ToString("N3");            

            this.BlockID = Core.Utilities.InsertBlock(location, 0, "ProposedLevel");
            BlockReference acBlkTblRec = trans.GetObject(this.BlockID, OpenMode.ForRead) as BlockReference;//this.BlockID.Open(OpenMode.ForRead) as BlockReference;
            foreach(ObjectId attId in acBlkTblRec.AttributeCollection)
            {
                AttributeReference attDef = trans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;

                if (attDef.Tag == "LEVEL")
                {
                    this.Text = attDef.ObjectId;
                    attDef.TextString = TextValue;
                    attDef.Modified += AttDef_Modified;
                }
            }
        }

        private void AttDef_Modified(object sender, EventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.EnteringQuiescentState += Editor_EnteringQuiescentState;            
        }

        private void Editor_EnteringQuiescentState(object sender, EventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.EnteringQuiescentState -= Editor_EnteringQuiescentState;

            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            using (DocumentLock dl = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
                {


                    AttributeReference attRef = trans.GetObject(this.Text, OpenMode.ForWrite) as AttributeReference;
                    string value = attRef.TextString;
                    if (value[0] == '@')
                    {
                        Absolute = true;
                        Level = double.Parse(value.Remove(0, 1));
                    }
                    else
                    {
                        Level = double.Parse(value);
                    }

                    attRef.TextString = TextValue;

                    trans.Commit();
                }
            }
        }

        public void Lock(Group group)
        {
            group.Append(BlockID);
        }
    }
}