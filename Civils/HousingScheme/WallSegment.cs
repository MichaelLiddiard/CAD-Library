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
    [Serializable]
    public class WallSegment
    {
        public string Guid;

        public WallJoint StartJoint;

        public Point3d StartPoint;

        public WallJoint EndJoint;

        public Point3d EndPoint;

        public long PerimeterLinePtr;

        public bool External;

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

        public void Generate()
        {

        }
    }
}
