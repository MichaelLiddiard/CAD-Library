using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
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
    /// Interaction logic for FoundationControl.xaml
    /// </summary>
    public partial class FoundationControl : UserControl
    {
        public FoundationControl()
        {
            InitializeComponent();
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            var Binding = this.DataContext as Plot;

            using (DocumentLock dl = acDoc.LockDocument())
            {
                for (int i = 0; i < Binding.WallSegments.Count; i++)
                {
                    using (Entity ent = Binding.WallSegments[i].ObjectId.Open(Autodesk.AutoCAD.DatabaseServices.OpenMode.ForWrite) as Entity)
                    {
                        if (dataGrid.SelectedIndex == i)
                        {
                            ent.Highlight();
                        } else
                        {
                            ent.Unhighlight();
                        }
                    }
                }

                Binding.Update();

                // Redraw the drawing
                Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
            }            
        }

        public void Hide()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            var Binding = this.DataContext as Plot;

            using (DocumentLock dl = acDoc.LockDocument())
            {
                for (int i = 0; i < Binding.WallSegments.Count; i++)
                {
                    using (Entity ent = Binding.WallSegments[i].ObjectId.Open(Autodesk.AutoCAD.DatabaseServices.OpenMode.ForWrite) as Entity)
                    {
                        ent.Unhighlight();
                    }
                }

                // Redraw the drawing
                Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.UpdateScreen();
            }
        }
        
    }
}
