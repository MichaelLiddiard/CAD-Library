using Autodesk.AutoCAD.ApplicationServices;
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
        /// <summary>
        /// Contains the pointer to the block inserted to display the levels
        /// </summary>
        public long BlockIDPtr;

        /// <summary>
        /// Direct link to the Autocad object created from the pointer to the block inserted to display the levels
        /// </summary>
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

        public bool LevelAccess { get; set; }

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
                    return (Parent.FinishedFloorLevel + Level).ToString("F3");
                }
            }
        }

        public double Param { get; set; }

        public PlotLevel()
        {

        }

        public PlotLevel(bool absolute, double Level, Plot Parent, double Parameter)
        {
            this.Absolute = absolute;
            this.Level = Level;
            this.Parent = Parent;
            Param = Parameter;

            //Check if access point?
            if(Level == 0)
            {
                this.LevelAccess = true;
            }
        }

        public void Generate(Point3d location)
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction trans = acCurDb.TransactionManager.TopTransaction;

            //string contents = (Parent.FinishedFloorLevel + ap.Offset).ToString("N3");            

            this.BlockID = Core.Utilities.InsertBlock(location, Parent.Rotation, "ProposedLevel");
            BlockReference acBlkTblRec = trans.GetObject(this.BlockID, OpenMode.ForRead) as BlockReference;//this.BlockID.Open(OpenMode.ForRead) as BlockReference;
            foreach(ObjectId attId in acBlkTblRec.AttributeCollection)
            {
                AttributeReference attDef = trans.GetObject(attId, OpenMode.ForWrite) as AttributeReference;

                if (attDef.Tag == "LEVEL")
                {
                    this.Text = attDef.ObjectId;
                    //Set to level offset otherwise event handler overrides
                    attDef.TextString = TextValue;//Level.ToString("F3");
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
                        double input = double.Parse(value);
                        //Ignore changes to an absolute value
                        if (Level != input)
                        {
                            Level = input - Parent.FinishedFloorLevel;
                            Absolute = false;
                        }
                    }

                    attRef.Modified -= AttDef_Modified;
                    attRef.TextString = TextValue;

                    Parent.GenerateHatching();

                    trans.Commit();

                    //attRef.Modified += AttDef_Modified;
                }
                //Dont know why but if this is part of the main transcation it crashes autocad. Needs to be reviewed at some stage
                //TODO: Review
                using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
                {
                    AttributeReference attRef = trans.GetObject(this.Text, OpenMode.ForWrite) as AttributeReference;
                    attRef.Modified += AttDef_Modified;
                }
            }
        }

        public void Update()
        {
            Application.DocumentManager.MdiActiveDocument.Editor.EnteringQuiescentState -= Editor_EnteringQuiescentState;

            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            using (DocumentLock dl = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
                {
                    AttributeReference attRef = trans.GetObject(this.Text, OpenMode.ForWrite) as AttributeReference;
                    attRef.Modified -= AttDef_Modified;
                    attRef.TextString = TextValue;

                    trans.Commit();
                }
                using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
                {
                    AttributeReference attRef = trans.GetObject(this.Text, OpenMode.ForWrite) as AttributeReference;
                    attRef.Modified += AttDef_Modified;
                }
            }
        }

        public void Lock(Group group)
        {
            group.Append(BlockID);
        }
    }
}
