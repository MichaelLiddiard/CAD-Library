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

        public List<WallSegment> Split(Point3d splitPoint)
        {
            List<WallSegment> result = new List<WallSegment>(2);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Transaction acTrans = acDoc.TransactionManager.TopTransaction;
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Line segment = acTrans.GetObject(PerimeterLine, OpenMode.ForWrite) as Line;

            Point3dCollection points = new Point3dCollection();
            points.Add(splitPoint);

            var splitLines = segment.GetSplitCurves(points);

            foreach (DBObject dbobj in splitLines)
            {
                Entity e = dbobj as Entity;

                WallSegment ws = new WallSegment() { PerimeterLine = acBlkTblRec.AppendEntity(e), Guid = System.Guid.NewGuid().ToString() };
                e.XData = new ResultBuffer(new TypedValue(1001, "JPP"), new TypedValue(1000, ws.Guid));
                acTrans.AddNewlyCreatedDBObject(e, true);

                result.Add(ws);
            }

            return result;
        }

        public void Erase()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Transaction acTrans = acDoc.TransactionManager.TopTransaction;
            BlockTable acBlkTbl;
            acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Line segment = acTrans.GetObject(PerimeterLine, OpenMode.ForWrite) as Line;
            segment.Erase();
        }

    }
}
