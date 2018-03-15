using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    interface IDrawingObject
    {
        ObjectId BaseObject { get; set; }

        Point3d Location { get; set; }

        bool Erased { get; }

        double Rotation { get; set; }
    }
}
