using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace JPP.Core
{
    public abstract class CircleObject : DrawingObject
    {
        public override Point3d Location
        {
            get
            {
                Transaction acTrans = Application.DocumentManager.MdiActiveDocument.TransactionManager.TopTransaction;
                Point3d result;
                using(Circle c = acTrans.GetObject(BaseObject, OpenMode.ForRead) as Circle)
                {
                    result = c.Center;
                }
                return result;
            }
            set
            {
                Transaction acTrans = Application.DocumentManager.MdiActiveDocument.TransactionManager.TopTransaction;
                (acTrans.GetObject(BaseObject, OpenMode.ForWrite) as Circle).Center = value;
            }
        }        

        public override double Rotation
        {
            get
            {
                return 0;
            }
            set
            {
                return;
            }
        }
    }
}
