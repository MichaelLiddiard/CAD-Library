using Autodesk.AutoCAD.ApplicationServices;
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
            //acDoc.SendStringToExecute("NewFFL ", false, false, false);
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
    }
}
