using Autodesk.AutoCAD.Windows;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using RibbonToggleButton = Autodesk.Windows.RibbonToggleButton;
using UserControl = System.Windows.Controls.UserControl;

namespace JPP.Core
{
    public class UIPanelToggle
    {
        readonly RibbonToggleButton _toggleButton;
        readonly PaletteSet _paletteSet;

        public UIPanelToggle(Autodesk.Windows.RibbonRowPanel parent, Bitmap buttonImage, string buttonText, Guid panelID, Dictionary<string,UserControl> controls)
        {

            _toggleButton = new RibbonToggleButton
            {
                ShowText = true,
                ShowImage = true,
                Text = buttonText
            };
            //toggleButton.Name = "Import As Xref";
            _toggleButton.CheckStateChanged += toggleButton_CheckStateChanged;
            _toggleButton.LargeImage = Core.Utilities.LoadImage(buttonImage);
            _toggleButton.Size = RibbonItemSize.Large;
            _toggleButton.Orientation = System.Windows.Controls.Orientation.Vertical;
            parent.Items.Add(_toggleButton);

            _paletteSet = new PaletteSet(buttonText, panelID);

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

                _paletteSet.Add(kv.Key, host);
            }

            _paletteSet.Size = new Size((int)maxWidth, (int)maxHeight);
            _paletteSet.Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton + (int)PaletteSetStyles.ShowCloseButton);
            _paletteSet.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);
                        
            _paletteSet.KeepFocus = false;
            _paletteSet.StateChanged += PaletteSet_StateChanged;
        }

        private void PaletteSet_StateChanged(object sender, PaletteSetStateEventArgs e)
        {
            if(e.NewState == StateEventIndex.Hide)
            {
                _toggleButton.CheckState = false;
            }
        }

        private void toggleButton_CheckStateChanged(object sender, EventArgs e)
        {
            
            if (_toggleButton.CheckState == true)
            {
                /*if (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument != null)
                {
                    uc2.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().Plots;
                    uc3.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().PlotTypes;
                }*/
                _paletteSet.Visible = true;
            }
            else
            {
                _paletteSet.Visible = false;
                /*uc2.DataContext = null;
                uc3.DataContext = null;*/
            }
        }
    }
}
