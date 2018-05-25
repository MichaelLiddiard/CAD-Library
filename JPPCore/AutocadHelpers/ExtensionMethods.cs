using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JPP.Core
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Stores that are currently loaded into memory
        /// </summary>
        private static Dictionary<string, DocumentStore> Stores = new Dictionary<string, DocumentStore>();

        /// <summary>
        /// Retrieve the document store to access embedded data in the specified document
        /// </summary>
        /// <typeparam name="T">Type of store to retrieve</typeparam>
        /// <param name="doc">The document for which to retrieve embedded data</param>
        /// <returns>The requested document store. If none is found a new instance is created</returns>
        public static T GetDocumentStore<T>(this Document doc) where T:DocumentStore
        {
            if(Stores.ContainsKey(doc.Name + typeof(T)))
            {                
                return (T)Stores[doc.Name + typeof(T)];
            } else
            {
                T ds = (T)Activator.CreateInstance(typeof(T), doc);
                Stores.Add(doc.Name + typeof(T), ds);
                doc.BeginDocumentClose += Doc_BeginDocumentClose;
                return ds;
            }
        }

        /// <summary>
        /// Retrieve the document store to access embedded data in the specified document
        /// </summary>
        /// <typeparam name="T">Type of store to retrieve</typeparam>
        /// <returns>The requested document store. If none is found a new instance is created</returns>
        public static T GetDocumentStore<T>(this Database db) where T:DocumentStore
        {
            //Check if resident in the cache
            //var document = Application.DocumentManager.GetDocument(db); This function doesnt work for side loaded db
            foreach (Document  document in Application.DocumentManager)
            {
                if (document.Database.Filename == db.Filename)
                {
                    return document.GetDocumentStore<T>();
                }
            }

            T ds = (T)Activator.CreateInstance(typeof(T), db);
            return ds;
        }

        private static void Doc_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            //When a document closes remove it from the store
            var currentStores = (from s in Stores where s.Key.Contains(((Document)sender).Name) select s.Key).ToArray();            
            foreach (string s in currentStores)
            {
                Stores.Remove(s);
            }
        }

        public static Arc GetArc(this CircularArc3d curve)
        {
            return new Arc(curve.Center, curve.Radius, curve.StartAngle, curve.EndAngle);
        }

        public static Arc Fillet(this Curve curve, Curve otherCurve, double radius)
        {
            Transaction tr = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument.Database.TransactionManager.TopTransaction;

            BlockTable acBlkTbl;
            acBlkTbl = tr.GetObject(Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

            BlockTableRecord acBlkTblRec;
            acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            Curve curveOffset;
            Curve curveOffsetsPlus = curve.GetOffsetCurves(radius)[0] as Curve;
            Curve curveOffsetsMinus = curve.GetOffsetCurves(-radius)[0] as Curve;
            if(curveOffsetsPlus.GetDistanceAtParameter(curveOffsetsPlus.EndParam) > curveOffsetsMinus.GetDistanceAtParameter(curveOffsetsMinus.EndParam))
            {
                curveOffset = curveOffsetsMinus;
            } else
            {
                curveOffset = curveOffsetsPlus;
            }

            Curve otherCurveOffset;
            Curve otherCurveOffsetsPlus = otherCurve.GetOffsetCurves(radius)[0] as Curve;
            Curve otherCurveOffsetsMinus = otherCurve.GetOffsetCurves(-radius)[0] as Curve;
            if (otherCurveOffsetsPlus.GetDistanceAtParameter(otherCurveOffsetsPlus.EndParam) > otherCurveOffsetsMinus.GetDistanceAtParameter(otherCurveOffsetsMinus.EndParam))
            {
                otherCurveOffset = otherCurveOffsetsMinus;
            }
            else
            {
                otherCurveOffset = otherCurveOffsetsPlus;
            }

            acBlkTblRec.AppendEntity(otherCurveOffset);
            tr.AddNewlyCreatedDBObject(otherCurveOffset, true);

            acBlkTblRec.AppendEntity(curveOffset);
            tr.AddNewlyCreatedDBObject(curveOffset, true);

            Point3d intersectionPoint;
            Point3dCollection intersections = new Point3dCollection();
            
            var tempIntersections = new Point3dCollection();
            curveOffset.IntersectWith(otherCurveOffset, Intersect.ExtendBoth, tempIntersections, IntPtr.Zero, IntPtr.Zero);
            intersectionPoint = tempIntersections[0];

            /*Circle c = new Circle(intersectionPoint, Vector3d.ZAxis, 50);
            acBlkTblRec.AppendEntity(c);
            tr.AddNewlyCreatedDBObject(c, true);*/

            //Calculate start and end point
            Point3d startPoint = curve.GetPointAtDist(radius);// / curve.GetDistanceAtParameter(curve.EndParam));
            Point3d endPoint = otherCurve.GetPointAtDist(radius); ;// otherCurve.GetDistanceAtParameter(otherCurve.EndParam));

            Circle c1 = new Circle(startPoint, Vector3d.ZAxis, 20);
            acBlkTblRec.AppendEntity(c1);
            tr.AddNewlyCreatedDBObject(c1, true);

            Circle c2 = new Circle(endPoint, Vector3d.ZAxis, 20);
            acBlkTblRec.AppendEntity(c2);
            tr.AddNewlyCreatedDBObject(c2, true);

            double startAngle, endAngle;

            startAngle = Vector3d.XAxis.GetAngleTo(intersectionPoint.GetVectorTo(startPoint), Vector3d.XAxis);
            endAngle = Vector3d.XAxis.GetAngleTo(intersectionPoint.GetVectorTo(endPoint), Vector3d.XAxis);

            return new Arc(intersectionPoint, radius, startAngle, endAngle);
        }
    }
}
