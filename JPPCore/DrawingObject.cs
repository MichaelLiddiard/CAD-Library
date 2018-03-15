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
    public abstract class DrawingObject : IDrawingObject
    {
        DBObject activeObject;

        public long BaseObjectPtr { get; set; }

        [XmlIgnore]
        public ObjectId BaseObject
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(BaseObjectPtr), 0);
            }
            set
            {
                BaseObjectPtr = value.Handle.Value;
                Transaction acTrans = Application.DocumentManager.MdiActiveDocument.TransactionManager.TopTransaction;
                activeObject = acTrans.GetObject(BaseObject, OpenMode.ForWrite);
                activeObject.Erased += ActiveObject_Erased;
                activeObject.Modified += ActiveObject_Modified;
            }
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

        public abstract Point3d Location { get; set; }
        
        public abstract double Rotation { get; set; }

        public DrawingObject()
        {
            
        }
    }
}
