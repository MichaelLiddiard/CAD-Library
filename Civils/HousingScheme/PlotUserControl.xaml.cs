using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

[assembly: CommandClass(typeof(JPP.Civils.PlotUserControl))]

namespace JPP.Civils
{
    /// <summary>
    /// Interaction logic for PlotUserControl.xaml
    /// </summary>
    public partial class PlotUserControl : UserControl
    {       
        public PlotUserControl()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            acDoc.SendStringToExecute("NewPlot ", false, false, false);
        }

        private void deletebutton_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            /*CivilDocumentStore cds = acDoc.GetDocumentStore<CivilDocumentStore>();
            string plotName = cds.Plots[dataGrid.SelectedIndex].PlotName;
            acDoc.SendStringToExecute("DeletePlot " + plotName + " ", false, false, false);*/
            acDoc.SendStringToExecute("DeletePlot ", false, false, false);
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void dataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            CivilDocumentStore cds = acDoc.GetDocumentStore<CivilDocumentStore>();


            using (DocumentLock dl = acDoc.LockDocument())
            {
                cds.Plots[dataGrid.SelectedIndex].Highlight();
                cds.Plots[dataGrid.SelectedIndex].Update();

                // Redraw the drawing
                Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
            }
        }

        [CommandMethod("NewPlot")]
        public static void NewPlot()
        {          
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("")
            {
                Message = "Enter plot type: "
            };

            foreach (PlotType pt in acDoc.GetDocumentStore<CivilDocumentStore>().PlotTypes)
            {
                pKeyOpts.Keywords.Add(pt.PlotTypeName);
            }
            pKeyOpts.AllowNone = false;
            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            string plotTypeId = pKeyRes.StringResult;

            PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter plot name: ") { AllowSpaces = true };
            PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);
            string plotId = pStrResPlot.StringResult;

            Plot p = new Plot
            {
                PlotName = plotId,
                PlotTypeId = plotTypeId
            };

            //Switch here for civil3d
            if (!Civils.Main.C3DActive)
            {
                //TODO: Remove requirement for Civil3d later
                throw new NotImplementedException();
                //Civil 3d not available so prompt for level
                PromptDoubleResult promptFFLDouble = acDoc.Editor.GetDouble("\nEnter the FFL: ");
                p.FinishedFloorLevel = promptFFLDouble.Value;
                p.UpdateLevelsFromSurface = false;
            }
            else
            {
                p.UpdateLevelsFromSurface = true;
            }

            PromptPointOptions pPtOpts = new PromptPointOptions("\nEnter base point of the plot: ");
            PromptPointResult pPtRes = acDoc.Editor.GetPoint(pPtOpts);
            p.BasePoint = pPtRes.Value;

            PromptPointOptions pAnglePtOpts = new PromptPointOptions("\nSelect point on base line: ");
            PromptPointResult pAnglePtRes = acDoc.Editor.GetPoint(pAnglePtOpts);
            Point3d p3d = pAnglePtRes.Value;
            double x, y;
            x = p3d.X - p.BasePoint.X;
            y = p3d.Y - p.BasePoint.Y;
            p.Rotation = Math.Atan(y / x);

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                try
                {
                    p.Generate();
                    acDoc.GetDocumentStore<CivilDocumentStore>().Plots.Add(p);

                    tr.Commit();
                } 

                /*p.Generate();

                if (Civils.Main.C3DActive)
                {
                    p.GetFFLfromSurface();
                }

                //TODO: This is horrendous but fuck it. Need to refactor to remove extra regen
                //p.Generate();
                p.Update();*/

                

                catch (ArgumentOutOfRangeException e)//(ArgumentOutOfRangeException e)
                {
                    acDoc.Editor.WriteMessage("\nSelected plot type corrupted. Please delete and recreate. Inner Exception:\n");
                    acDoc.Editor.WriteMessage(e.Message);
                }                
            }            
        }
    }
}
