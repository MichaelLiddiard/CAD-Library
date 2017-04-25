using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Windows;

namespace JPPCommands
{
    public static partial class AddFFL
    {
        public static bool AddFFLData(ObjectId outlineId)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Editor acEditor = acDoc.Editor;

            // Each building outline will have an extension dictionary which contains the following XRecords....
            //
            // FFL:         Name - "FFL"
            //              Data - double FFL
            //
            // Rotation:    Name - "Rotation"
            //              Data - double rotation_angle
            //
            // Vertex:      Name - "Vertex_X", where X is the index
            //              Data - integer Index
            //              Data - string "Corner" or "Access"
            //              Data - string "At_FFL" or "At_150_Down" or "Defined_Level"
            //              Data - double Level
            //
            // Add an extension dictionary to the outline
            //
            if (!JPPUtils.addExtensionDictionary(outlineId))
                return false;
            double FFLDouble;           // Declare here to ensure in scope for later
            PromptDoubleResult promptFFLDouble = acEditor.GetDouble("\nEnter the FFL: ");
            if (promptFFLDouble.Status == PromptStatus.OK)
            {
                FFLDouble = promptFFLDouble.Value;
                ResultBuffer xrecFFLData = new ResultBuffer();
                xrecFFLData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, FFLDouble));
                if (!JPPUtils.addXrecord(outlineId, "FFL", xrecFFLData))
                {
                    acEditor.WriteMessage("\nError: could not add Xrecord: FFL.");
                    return false;
                }
                // The rotation will be used as the basis for placing the FFL text. Fetch the angle
                // of the first vector to the x-axis and create a Xrecord to store it
                ResultBuffer xrecAngleData = new ResultBuffer();
                double? angleOfFirstSegment = getAngle(outlineId);
                if (angleOfFirstSegment == null)
                    return false;
                xrecAngleData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, angleOfFirstSegment));
                if (!JPPUtils.addXrecord(outlineId, "Rotation", xrecAngleData))
                {
                    acEditor.WriteMessage("\nError: could not add Xrecord: Rotation.");
                    return false;
                }
                // Add the corner points XRecords
                int? NoOfVertices = getNoOfVertices(outlineId);
                if (NoOfVertices == null)
                    return false;
                for (int index = 0; index < NoOfVertices; index++)
                {
                    string xrecName = "Vertex_" + index.ToString();
                    ResultBuffer xrecVertexData = new ResultBuffer();
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, index));
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Corner"));
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Defined_Level"));
                    // Add the default level at 150mm below FFL
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, (FFLDouble - 0.15)));
                    if (!JPPUtils.addXrecord(outlineId, xrecName, xrecVertexData))
                    {
                        acEditor.WriteMessage("\nError: could not add Xrecord: Vertex_" + index +".");
                        return false;
                    }
                }
                return true;
            }
            else if (promptFFLDouble.Status == PromptStatus.Cancel)
            {
                acEditor.WriteMessage("\nDefine FFL request cancelled.");
                return false;
            }
            else
            {
                acEditor.WriteMessage("\nError entering FFL.");
                return false;
            }
        }

        private static double? getAngle(ObjectId plineOutlineId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {

                double? angle = null;
                try
                {
                    Polyline outline = acTrans.GetObject(plineOutlineId, OpenMode.ForWrite) as Polyline;
                    angle = outline.GetPoint2dAt(0).GetVectorTo(outline.GetPoint2dAt(1)).Angle;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError getting angle of first segment of outline.\n");
                }
                acTrans.Commit();
                return angle;
            }
        }

        private static int? getNoOfVertices(ObjectId plineOutlineId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {

                int? NumberOfVertices = null;
                try
                {
                    Polyline outline = acTrans.GetObject(plineOutlineId, OpenMode.ForWrite) as Polyline;
                    NumberOfVertices = outline.NumberOfVertices;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError getting number of vertices of outline.\n");
                }
                acTrans.Commit();
                return NumberOfVertices;
            }
        }
    }
}