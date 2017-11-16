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
using System.Windows.Forms.Integration;
using System.Windows.Forms;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Diagnostics;
using System.Configuration;

[assembly: ExtensionApplication(typeof(JPP.Core.JPPMain))]
[assembly: CommandClass(typeof(JPP.Core.JPPMain))]

namespace JPP.Core
{
    /// <summary>
    /// Loader class, the main entry point for the full application suite. Implements IExtensionApplication is it automatically initialised and terminated by AutoCad.
    /// </summary>
    public class JPPMain : IExtensionApplication
    {
        public static PaletteSet _ps;
        public static RibbonToggleButton settingsButton;

        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need. Main library entry point
        /// </summary>
        public void Initialize()
        {
            //Upgrade and load the app settings
            Properties.Settings.Default.Upgrade();

            //Detect if ribbon is currently loaded, and if not wait until the application is Idle.
            //Throws an error if try to add to the menu with the ribbon unloaded
            if (ComponentManager.Ribbon == null)
            {
                Application.Idle += Application_Idle;
                //ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;
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
            throw new NotImplementedException();
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
            if (!RegistryHelper.IsAutoload())
            {
                //No autoload found
                TaskDialog autoloadPrompt = new TaskDialog();
                autoloadPrompt.WindowTitle = Constants.Friendly_Name;
                autoloadPrompt.MainInstruction = "JPP Library does not currently load automatically. Would you like to enable this?";
                autoloadPrompt.MainIcon = TaskDialogIcon.Information;
                autoloadPrompt.FooterText = "May cause unexpected behaviour on an unsupported version of Autocad";
                autoloadPrompt.FooterIcon = TaskDialogIcon.Warning;
                autoloadPrompt.Buttons.Add(new TaskDialogButton(0, "No, continue without"));
                autoloadPrompt.Buttons.Add(new TaskDialogButton(1, "Enable autoload"));
                autoloadPrompt.DefaultButton = 0;
                autoloadPrompt.Callback = delegate (ActiveTaskDialog atd, TaskDialogCallbackArgs e, object sender)
                {
                    if (e.Notification == TaskDialogNotification.ButtonClicked)
                    {
                        if (e.ButtonId == 1)
                        {
                            //TODO: Disable when registry is ok
                            //RegistryHelper.CreateAutoload();
                            Application.ShowAlertDialog("Autload creation currently disabled.");
                        }
                    }
                    return false;
                };
                autoloadPrompt.Show(Application.MainWindow.Handle);
            } //Core autloads, progress

            //Create the main UI
            RibbonTab JPPTab = CreateTab();
            CreateCoreMenu(JPPTab);

            //Load the additional DLL files
#if !DEBUG
            Update();
            LoadModules();
#endif
            //Create settings window
            _ps = new PaletteSet("JPP", new Guid("9dc86012-b4b2-49dd-81e2-ba3f84fdf7e3"));
            _ps.Size = new Size(600, 800);
            _ps.Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton + (int)PaletteSetStyles.ShowCloseButton);
            _ps.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);

            SettingsUserControl suc = new SettingsUserControl();
            ElementHost host1 = new ElementHost();
            host1.AutoSize = true;
            host1.Dock = DockStyle.Fill;
            host1.Child = new SettingsUserControl();
            _ps.Add("Settings", host1);

            _ps.KeepFocus = false;
            //_ps.Visible = true;
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

            settingsButton = new RibbonToggleButton();//Utilities.CreateButton("Settings", Properties.Resources.settings, RibbonItemSize.Standard, "");            
            settingsButton.ShowText = true;
            settingsButton.ShowImage = true;
            settingsButton.Text = "Settings";
            settingsButton.Name = "Display the settings window";
            settingsButton.CheckStateChanged += settingsButton_CheckStateChanged;
            settingsButton.Image = Core.Utilities.LoadImage(Properties.Resources.settings);
            settingsButton.Size = RibbonItemSize.Standard;
            settingsButton.Orientation = System.Windows.Controls.Orientation.Horizontal;
            stack.Items.Add(settingsButton);
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
            stack.Items.Add(runLoad);*/

            //Add the new tab section to the main tab
            source.Items.Add(stack);
            Panel.Source = source;
            JPPTab.Panels.Add(Panel);
        }

        private static void settingsButton_CheckStateChanged(object sender, EventArgs e)
        {
            if(settingsButton.CheckState == true)
            {
                _ps.Visible = true;
            } else
            {
                _ps.Visible = false;
            }
        }

        //TODO: Fix this method, and make more functional
        /// <summary>
        /// Create the pallette set window to which individual panels get added
        /// </summary>
        public static void CreateWindow()
        {
            /*PaletteSet _ps = new PaletteSet("WPF Palette");
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
            _ps.Visible = true;*/
        }

        /// <summary>
        /// Find all assemblies in the subdirectory, and load them into memory
        /// </summary>
        private static void LoadModules()
        {
            List<string> allAssemblies = new List<string>();
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library";

            //Check if authenticated, otherwise block the auto loading
            if (Authentication.Current.Authenticated())
            {
                //Iterate over every dll found in bin folder
                if (Directory.Exists(path))
                {
                    foreach (string dll in Directory.GetFiles(path, "*.dll"))
                    {
                        string dllPath = dll.Replace('\\', '/');
                        //Load the additional libraries found
                        ExtensionLoader.Load(dll);
                    }
                }
            }
        }

        /// <summary>
        /// Find all assemblies in the subdirectory, and load them into memory
        /// </summary>
        private static void PrepareDrawing()
        {
            //Add the default JPP handler


        }

        //TODO: Trigger update method somehow
        [CommandMethod("Update")]
        public static void Update()
        {
            bool updateRequired = false;
            bool installUpdateRequired = false;
            string archivePath;
            string installerPath = "";

            //Get manifest file from known location
            if (File.Exists("M:\\ML\\CAD-Library\\manifest.txt"))
            {
                using (TextReader tr = File.OpenText("M:\\ML\\CAD-Library\\manifest.txt"))
                {
                    //Currently manifest file contians version of zip file to pull data from
                    archivePath = Constants.ArchivePath + tr.ReadLine() + ".zip";
                    if (tr.Peek() != -1)
                    {
                        installerPath = "M:\\ML\\CAD-Library\\" + tr.ReadLine() + ".exe";
                    }
                }
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library\\manifest.txt"))
                {
                    using (TextReader tr = File.OpenText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library\\manifest.txt"))
                    {
                        //Currently manifest file contians version of zip file to pull data from
                        if (archivePath != Constants.ArchivePath + tr.ReadLine() + ".zip")
                        {
                            updateRequired = true;
                        }
                        if (tr.Peek() != -1)
                        {
                            if (installerPath != "M:\\ML\\CAD-Library\\" + tr.ReadLine() + ".exe")
                            {
                                installUpdateRequired = true;
                            }
                        }
                    }
                }
                else
                {
                    updateRequired = true;
                    installUpdateRequired = true;
                }

                //Get the current version for comparison

                using (StreamWriter sw = new StreamWriter(File.Open(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library\\manifest.txt", FileMode.Create)))
                {

                    //Download the latest resources update
                    try
                    {
                        if (updateRequired)
                        {
                            ZipArchive archive = ZipFile.OpenRead(archivePath);

                            //string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bin";
                            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library";
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                entry.ExtractToFile(Path.Combine(path, entry.FullName), true);
                            }

                            sw.WriteLine(archivePath);
                        }

                        //if there is a new installer...
                        if (installerPath != "" && installUpdateRequired)
                        {
                            TaskDialog autoloadPrompt = new TaskDialog();
                            autoloadPrompt.WindowTitle = Constants.Friendly_Name;
                            autoloadPrompt.MainInstruction = "A new version of the application has been found. Would you like to install now?";
                            autoloadPrompt.MainIcon = TaskDialogIcon.Information;
                            autoloadPrompt.Buttons.Add(new TaskDialogButton(0, "Exit and install"));
                            autoloadPrompt.Buttons.Add(new TaskDialogButton(1, "Not right now"));
                            autoloadPrompt.DefaultButton = 0;
                            autoloadPrompt.Callback = delegate (ActiveTaskDialog atd, TaskDialogCallbackArgs e, object sender)
                            {
                                if (e.Notification == TaskDialogNotification.ButtonClicked)
                                {
                                    if (e.ButtonId == 0)
                                    {
                                        Process.Start(installerPath);
                                        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("quit ", true, false, true);
                                        //Application.Quit();
                                    }
                                }
                                return false;
                            };
                            autoloadPrompt.Show(Application.MainWindow.Handle);

                            sw.WriteLine(installerPath);
                        }
                    }
                    catch (System.Exception e)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
