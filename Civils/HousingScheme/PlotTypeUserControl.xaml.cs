using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
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
    public partial class PlotTypeUserControl : UserControl
    {       
        public PlotTypeUserControl()
        {
            InitializeComponent();
            PlotType.OnCurrentOpenChanged += PlotType_OnCurrentOpenChanged;
            libraryTree.ItemsSource = Civils.Main.ptLibrary.Tree;                     
        }

        private void PlotType_OnCurrentOpenChanged()
        {
            if (PlotType.CurrentOpen != null)
            {
                this.DataContext = PlotType.CurrentOpen;
                PlotCommands.IsEnabled = true;
            }
            else
            {
                this.DataContext = null;
                PlotCommands.IsEnabled = false;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            acDoc.SendStringToExecute("PT_Create ", false, false, false);
        }

        private void deletebutton_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            //acDoc.SendStringToExecute("NewFFL ", false, false, false);
        }

        private void wallbutton_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            acDoc.SendStringToExecute("PT_CreateWS ", false, false, false);
        }

        private void doorbutton_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            acDoc.SendStringToExecute("PT_AddAccess ", false, false, false);
        }

        private void finalisebutton_Click(object sender, RoutedEventArgs e)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            acDoc.SendStringToExecute("PT_Finalise ", false, false, false);
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            if(libraryTree.SelectedItem is Leaf)
            {
                Leaf selected = libraryTree.SelectedItem as Leaf;
                using (Database source = selected.GetDatabase())
                {
                    Database target = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument.Database;
                    //PlotType.Transfer(selected.Name, target, source);
                    PlotType sourceType = Civils.Main.ptLibrary.GetLeafEntity(selected);
                    sourceType.SaveTo(selected.Name, target);
                }
            }
            MessageBox.Show("Please select a valid item to load");
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            PlotType instance = plotTypeGrid.SelectedItem as PlotType;
            Civils.Main.ptLibrary.SaveLeafEntity(instance.PlotTypeName, instance);            
        }

        private void plotTypeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Civils.Main.ptLibrary.Tree.Count > 0)
            {
                saveButton.IsEnabled = true;
            }
        }

        private void libraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (libraryTree.SelectedItem is Leaf)
            {
                loadButton.IsEnabled = true;
            } else
            {
                loadButton.IsEnabled = false;
            }
        }
    }
}
