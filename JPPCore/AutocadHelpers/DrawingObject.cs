using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Xml.Serialization;

namespace JPP.Core
{
    public abstract class DrawingObject : IDrawingObject
    {
        [XmlIgnore]
        DBObject activeObject;

        public long BaseObjectPtr { get; set; }

        [XmlIgnore]
        public ObjectId BaseObject
        {
            get
            {
                if (BaseObjectPtr != 0)
                {
                    Document acDoc = Application.DocumentManager.MdiActiveDocument;
                    Database acCurDb = acDoc.Database;
                    return acCurDb.GetObjectId(false, new Handle(BaseObjectPtr), 0);
                }
                else
                {
                    throw new NullReferenceException("No base object has been linked");
                }
            }
            set
            {
                BaseObjectPtr = value.Handle.Value;
                CreateActiveObject();
            }
        }

        public void CreateActiveObject()
        {
            Transaction acTrans = Application.DocumentManager.MdiActiveDocument.TransactionManager.TopTransaction;
            activeObject = acTrans.GetObject(BaseObject, OpenMode.ForWrite);
            activeObject.Erased += ActiveObject_Erased;
            activeObject.Modified += ActiveObject_Modified;
        }

        public abstract void ActiveObject_Modified(object sender, EventArgs e);

        public void ActiveObject_Erased(object sender, ObjectErasedEventArgs e)
        {
            _Erased = true;
        }

        bool _Erased = false;

        public bool Erased
        {
            get
            {
                return _Erased;
            }
        }

        [XmlIgnore]
        public abstract Point3d Location { get; set; }

        public abstract void Generate();

        [XmlIgnore]
        public abstract double Rotation { get; set; }

        public DrawingObject()
        {

        }
    }
}
