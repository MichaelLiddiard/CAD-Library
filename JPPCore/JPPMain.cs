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
using Autodesk.AutoCAD.Windows;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(JPP.Core.JPPMain))]
[assembly: CommandClass(typeof(JPP.Core.JPPMain))]

namespace JPP.Core
{
    /// <summary>
    /// Loader class, the main entry point for the full application suite. Implements IExtensionApplication is it automatically initialised and terminated by AutoCad.
    /// </summary>
    public class JPPMain : IExtensionApplication
    {
        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need. Main library entry point
        /// </summary>
        public void Initialize()
        {
            //Detect if ribbon is currently loaded, and if not wait until the application is Idle.
            //Throws an error if try to add to the menu with the ribbon unloaded
            if (ComponentManager.Ribbon == null)
            {
                Application.Idle += Application_Idle;
            }
            else
            {
                //Ribbon existis, call the initialize method directly
                InitJPP();
            }
        }

        /// <summary>
        /// Implement the Autocad extension api to terminate the application
        /// </summary>
        public void Terminate()
        {
            //No specific termination code required as of yet
        }

        /// <summary>
        /// Event handler to detect when the program is fully loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Idle(object sender, EventArgs e)
        {
            //Unhook the event handler to prevent multiple calls
            Application.Idle -= Application_Idle;
            //Call the initialize method now the document is loaded
            InitJPP();
        }

        /// <summary>
        /// Init JPP command loads all essential elements of the program, including the helper DLL files.
        /// </summary>
        [CommandMethod("InitJPP")]        
        public static void InitJPP()
        {
            //Check for registry key for autoload
            if(!RegistryHelper.IsAutoload())
            {
                //No autoload found
                TaskDialog autoloadPrompt = new TaskDialog();
                autoloadPrompt.WindowTitle = Constants.Friendly_Name;
                autoloadPrompt.MainInstruction = "No autoload setting has been found. Would you like one to be created?";
                autoloadPrompt.MainIcon = TaskDialogIcon.Information;
                autoloadPrompt.FooterText = "WARNING: May cause unexpected behaviour on an unsupported version of Autocad";
                autoloadPrompt.FooterIcon = TaskDialogIcon.Warning;
                autoloadPrompt.Buttons.Add(new TaskDialogButton(0, "Continue Without"));
                autoloadPrompt.Buttons.Add(new TaskDialogButton(1, "Create Autload Setting"));                
                autoloadPrompt.DefaultButton = 0;
                autoloadPrompt.Callback = delegate(ActiveTaskDialog atd, TaskDialogCallbackArgs e, object sender)
                {
                    if(e.ButtonId == 1)
                    {
                        RegistryHelper.CreateAutoload();
                    }
                    return false;
                };
                autoloadPrompt.Show(Application.MainWindow.Handle);
            } //Core autloads, progress

            //Create the main UI
            RibbonTab JPPTab = CreateTab();
            CreateCoreMenu(JPPTab);                       

            //Load the additional DLL files
            LoadAssemblies();
        }

        /// <summary>
        /// Creates the JPP tab and adds it to the ribbon
        /// </summary>
        /// <returns>The created tab</returns>
        public static RibbonTab CreateTab()
        {
            RibbonControl rc = Autodesk.Windows.ComponentManager.Ribbon;
            RibbonTab JPPTab = new RibbonTab();

            //Pull names from constant file as used in all subsequent DLL's
            JPPTab.Name = Constants.JPP_Tab_Title;
            JPPTab.Title = Constants.JPP_Tab_Title;
            JPPTab.Id = Constants.JPP_Tab_ID;

            rc.Tabs.Add(JPPTab);
            return JPPTab;
        }

        /// <summary>
        /// Add the core elements of the ui
        /// </summary>
        /// <param name="JPPTab">The tab to add the ui elements to</param>
        public static void CreateCoreMenu(RibbonTab JPPTab)
        {
            RibbonPanel Panel = new RibbonPanel();
            RibbonPanelSource source = new RibbonPanelSource();
            source.Title = "Main";

            RibbonRowPanel stack = new RibbonRowPanel();

            //RibbonButton authenticateButton = new RibbonButton();
            RibbonButton authenticateButton = Utilities.CreateButton("Authenticate", Properties.Resources.Locked, RibbonItemSize.Standard, "");
            stack.Items.Add(authenticateButton);
            stack.Items.Add(new RibbonRowBreak());

            //Add button to update all JPP libraries
            /*RibbonButton runLoad = new RibbonButton();
            runLoad.ShowText = true;
            runLoad.Text = "Update";
            runLoad.Name = "Check for updates";
            runLoad.CommandHandler = new RibbonCommandHandler();
            runLoad.CommandParameter = "._Update ";
#if DEBUG
            runLoad.IsEnabled = false;
#endif
            stack.Items.Add(runLoad);
            source.Items.Add(stack);*/

            //Add the new tab section to the main tab
            Panel.Source = source;
            JPPTab.Panels.Add(Panel);
        }

        //TODO: Fix this method, and make more functional
        /// <summary>
        /// Create the pallette set window to which individual panels get added
        /// </summary>
        public static void CreateWindow()
        {
            PaletteSet _ps = new PaletteSet("WPF Palette");
            _ps.Size = new Size(400, 600);
            _ps.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);

            PlotUserControl uc2 = new PlotUserControl();
            uc2.DataContext = DocumentStore.Current.Plots;
            ElementHost host = new ElementHost();
            host.AutoSize = true;
            host.Dock = DockStyle.Fill;
            host.Child = uc2;
            _ps.Add("Add ElementHost", host);

            // Display our palette set
            _ps.KeepFocus = true;
            _ps.Visible = true;
        }

        /// <summary>
        /// Find all assemblies in the subdirectory, and load them into memory
        /// </summary>
        private static void LoadAssemblies()
        {
            List<string> allAssemblies = new List<string>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bin";

            //Check if authenticated, otherwise block the auto loading
            if (Authentication.Current.Authenticated())
            {
                //Iterate over every dll found in bin folder
                foreach (string dll in Directory.GetFiles(path, "*.dll"))
                {
                    string dllPath = dll.Replace('\\', '/');
                    //Load the additional libraries found
                    ExtensionLoader.Load(dll);
                }
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

                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\update";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    entry.ExtractToFile(Path.Combine(path, entry.FullName), true);
                }

                LoadAssemblies();
            }
            catch (System.Exception e)
            {
                throw new NotImplementedException();
            }
        }
        
        
    }
}
