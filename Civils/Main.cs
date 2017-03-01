using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    class Main : IExtensionApplication
    {
        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need
        /// </summary>
        public void Initialize()
        {            
            /*//Add the menu options
            RibbonControl rc = Autodesk.Windows.ComponentManager.Ribbon;
            RibbonTab JPPTab = new RibbonTab();
            JPPTab.Name = "JPP";
            JPPTab.Title = "JPP";
            JPPTab.Id = "JPPCORE_JPP_TAB";

            RibbonPanel Panel = new RibbonPanel();
            RibbonPanelSource source = new RibbonPanelSource();
            source.Title = "Common";

            //Add button to re load all JPP libraries
            RibbonButton runLoad = new RibbonButton();
            runLoad.ShowText = true;
            runLoad.Text = "Update";
            runLoad.Name = "Check for updates";
            runLoad.CommandHandler = new RibbonCommandHandler();
            runLoad.CommandParameter = "._Update ";
            source.Items.Add(runLoad);

            //Build the UI hierarchy
            Panel.Source = source;
            JPPTab.Panels.Add(Panel);
            rc.Tabs.Add(JPPTab);*/
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
