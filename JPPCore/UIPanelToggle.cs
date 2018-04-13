using Autodesk.AutoCAD.Customization;
using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using RibbonToggleButton = Autodesk.Windows.RibbonToggleButton;
using UserControl = System.Windows.Controls.UserControl;

namespace JPP.Core
{
    public class UIPanelToggle
    {
        RibbonToggleButton toggleButton;
        PaletteSet paletteSet;

        public UIPanelToggle(Autodesk.Windows.RibbonRowPanel parent, Bitmap buttonImage, string buttonText, Guid panelID, Dictionary<string,UserControl> controls)
        {
            
            toggleButton = new RibbonToggleButton();
            
            toggleButton.ShowText = true;
            toggleButton.ShowImage = true;
            toggleButton.Text = buttonText;
            //toggleButton.Name = "Import As Xref";
            toggleButton.CheckStateChanged += toggleButton_CheckStateChanged;
            toggleButton.LargeImage = Core.Utilities.LoadImage(buttonImage);
            toggleButton.Size = RibbonItemSize.Large;
            toggleButton.Orientation = System.Windows.Controls.Orientation.Vertical;
            parent.Items.Add(toggleButton);

            paletteSet = new PaletteSet(buttonText, panelID);

            double maxWidth = 0;
            double maxHeight = 0;
            foreach(KeyValuePair<string,UserControl> kv in controls)
            {
                UserControl uc = kv.Value;

                if (uc.Width > maxWidth)
                    maxWidth = uc.Width;

                if (uc.Height > maxHeight)
                    maxHeight = uc.Height;

                ElementHost host = new ElementHost();
                host.AutoSize = true;
                host.Dock = DockStyle.Fill;
                host.Child = uc;

                paletteSet.Add(kv.Key, host);
            }

            paletteSet.Size = new Size((int)maxWidth, (int)maxHeight);
            paletteSet.Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton + (int)PaletteSetStyles.ShowCloseButton);
            paletteSet.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);
                        
            paletteSet.KeepFocus = false;
            paletteSet.StateChanged += PaletteSet_StateChanged;
        }

        private void PaletteSet_StateChanged(object sender, PaletteSetStateEventArgs e)
        {
            if(e.NewState == StateEventIndex.Hide)
            {
                toggleButton.CheckState = false;
            }
        }

        private void toggleButton_CheckStateChanged(object sender, EventArgs e)
        {
            
            if (toggleButton.CheckState == true)
            {
                /*if (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument != null)
                {
                    uc2.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().Plots;
                    uc3.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().PlotTypes;
                }*/
                paletteSet.Visible = true;
            }
            else
            {
                paletteSet.Visible = false;
                /*uc2.DataContext = null;
                uc3.DataContext = null;*/
            }
        }
    }
}
