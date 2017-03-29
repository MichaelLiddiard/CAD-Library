using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using System.IO.Compression;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.DatabaseServices;

[assembly: ExtensionApplication(typeof(JPP.Core.Loader))]
[assembly: CommandClass(typeof(JPP.Core.Loader))]

namespace JPP.Core
{
    /// <summary>
    /// Loader class, the main entry point for the full application suite. Implements IExtensionApplication is it automatically initialised and terminated by AutoCad.
    /// </summary>
    public class Loader : IExtensionApplication
    {
        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need
        /// </summary>
        public void Initialize()
        {
#if DEBUG
            Application.ShowAlertDialog("Running in debug mode.");
#endif
            //Detect if ribbon is currently loaded, and if not wait until the application is Idle.
            if (ComponentManager.Ribbon == null)
            {
                Application.Idle += Application_Idle;
            }
            else
            {
                //Ribbon existis, call the initialize methods
                InitJPP(); 
            }
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            //Unhook the event handler
            Application.Idle -= Application_Idle;
            //Call the initialize method
            InitJPP();
        }

        private void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
        {
            Application.ShowAlertDialog("triggered");
            if (ComponentManager.Ribbon != null)
            {
                Application.ShowAlertDialog("Ribbon active");
                InitJPP();
                ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
            }            
        }

        /// <summary>
        /// Implement the Autocad extension api to terminate the application
        /// </summary>
        public void Terminate()
        {
            //No specific termination code required as of yet
        }
                
        private static void Load()
        {
            List<string> allAssemblies = new List<string>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bin";

            //Iterate over every dll found in bin folder
            foreach (string dll in Directory.GetFiles(path, "*.dll"))
            {
                string dllPath = dll.Replace('\\', '/');
                //Load the additional libraries found
                ExtensionLoader.Load(dll);                
            }            
        }

        [CommandMethod("Update")]
        public static void Update()
        {          
            string archivePath;
            //Get manifest fiel from known location
            using (TextReader tr = File.OpenText("M:\\ML\\CAD-Library\\manifest.txt"))
            {
                //Currently manifest file contians version of zip file to pull data from
                archivePath = Constants.ArchivePath + tr.ReadToEnd() + ".zip";
            }

            //Download the latest resources update
            try
            {
                ZipArchive archive = ZipFile.OpenRead(archivePath);
                
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bin";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    entry.ExtractToFile(Path.Combine(path, entry.FullName), true);
                }

                Load();
            }
            catch (System.Exception e)
            {
                throw new NotImplementedException();
            }
        }

        [CommandMethod("InitJPP")]
        public static void InitJPP()
        {
            //Check current folder for zip file waiting to be applied

            RibbonTab JPPTab = CreateTab();

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

            //Not sure why but something in the next three lines crashes the addin when auto loaded from init
            //Build the UI hierarchy
            Panel.Source = source;
            JPPTab.Panels.Add(Panel);

            Update();
            Load();
        }

        public static RibbonTab CreateTab()
        {
            //Add the menu options
            RibbonControl rc = Autodesk.Windows.ComponentManager.Ribbon;
            RibbonTab JPPTab = new RibbonTab();
            JPPTab.Name = "JPP";
            JPPTab.Title = "JPP";
            JPPTab.Id = "JPPCORE_JPP_TAB";
            rc.Tabs.Add(JPPTab);
            return JPPTab;
        }
    }
}
