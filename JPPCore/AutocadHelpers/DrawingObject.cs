using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Xml.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace JPP.Core
{
    public abstract class DrawingObject : IDrawingObject
    {
        [XmlIgnore]
        DBObject _activeObject;

        long BaseObjectPtr { get; set; }

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
            _activeObject = acTrans.GetObject(BaseObject, OpenMode.ForWrite);
            _activeObject.Erased += ActiveObject_Erased;
            _activeObject.Modified += ActiveObject_Modified;
        }

        protected abstract void ActiveObject_Modified(object sender, EventArgs e);

        void ActiveObject_Erased(object sender, ObjectErasedEventArgs e)
        {
            Erased = true;
        }

        public bool Erased { get; private set; }

        [XmlIgnore]
        public abstract Point3d Location { get; set; }

        public abstract void Generate();

        [XmlIgnore]
        public abstract double Rotation { get; set; }
    }
}
