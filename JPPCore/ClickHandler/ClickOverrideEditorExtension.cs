using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;

namespace JPP.Core
{
    public static class ClickOverrideEditorExtension
    {
        public static PromptSelectionResult SelectAtPickBox(this Editor ed, Point3d pickBoxCentre)
        {
            //Get pick box's size on screen
            System.Windows.Point screenPt = ed.PointToScreen(pickBoxCentre, 1);

            //Get pickbox's size. Note, the number obtained from
            //system variable "PICKBOX" is actually the half of
            //pickbox's width/height
            object pBox = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("PICKBOX");
            int pSize = Convert.ToInt32(pBox);

            //Define a Point3dCollection for CrossingWindow selecting
            Point3dCollection points = new Point3dCollection();

            System.Drawing.Point p;
            Point3d pt;

            p = new System.Drawing.Point((int)screenPt.X - pSize, (int)screenPt.Y - pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            p = new System.Drawing.Point((int)screenPt.X + pSize, (int)screenPt.Y - pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            p = new System.Drawing.Point((int)screenPt.X + pSize, (int)screenPt.Y + pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            p = new System.Drawing.Point((int)screenPt.X - pSize, (int)screenPt.Y + pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            return ed.SelectCrossingPolygon(points);
        }
    }
}
