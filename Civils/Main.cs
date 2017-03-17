using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

[assembly: ExtensionApplication(typeof(JPP.Civils.Main))]

namespace JPP.Civils
{
    public class Main : IExtensionApplication
    {
        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need
        /// </summary>
        public void Initialize()
        {
            //Add the menu options
            RibbonControl rc = Autodesk.Windows.ComponentManager.Ribbon;
            RibbonTab JPPTab = rc.FindTab("JPPCORE_JPP_TAB");
            if(JPPTab == null)
            {
                JPPTab = JPP.Core.Loader.CreateTab();
            }

            RibbonPanel Panel = new RibbonPanel();
            RibbonPanelSource source = new RibbonPanelSource();
            source.Title = "Civils";            

            //Add button to re load all JPP libraries
            RibbonButton layPipeButton = new RibbonButton();
            layPipeButton.ShowText = true;
            layPipeButton.Text = "Lay Pipe";
            layPipeButton.Name = "Lay Pipe";
            layPipeButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            layPipeButton.CommandParameter = "._LayPipe ";        
            //runLoad.Image = (ImageSource)ic.ConvertFrom(JPP.Civils.Properties.Resources.pipeIcon);
            source.Items.Add(layPipeButton);

            //Not sure why but something in the next three lines crashes the addin when auto loaded from init
            //Build the UI hierarchy
            Panel.Source = source;
            JPPTab.Panels.Add(Panel);

        }

        /// <summary>
        /// Implement the Autocad extension api to terminate the application
        /// </summary>
        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
