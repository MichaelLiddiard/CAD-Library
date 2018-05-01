using System;
using System.Collections.Generic;
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

[assembly: ExtensionApplication(typeof(JPP.Core.JPPMain))]
[assembly: CommandClass(typeof(JPP.Core.JPPMain))]

namespace JPP.Core
{
    /// <summary>
    /// Loader class, the main entry point for the full application suite. Implements IExtensionApplication is it automatically initialised and terminated by AutoCad.
    /// </summary>
    public class JPPMain : IExtensionApplication
    {

        #region Private variables
        /// <summary>
        /// PaletteSet containing the settings window
        /// </summary>
        private static PaletteSet _settingsWindow;

        /// <summary>
        /// Ribon toggle button for displaying settings window
        /// </summary>
        private static RibbonToggleButton _settingsButton;

        /// <summary>
        /// Keep a reference to handler to prevent GC
        /// </summary>
        private static ClickOverride ClickOverride;
        #endregion

        #region Autocad Extension Lifecycle
        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need. Main library entry point
        /// </summary>
        public void Initialize()
        {
            //Upgrade and load the app settings
            //TODO: Verify this is actually required
            Properties.Settings.Default.Upgrade();

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
            //Call the initialize method now the application is loaded
            InitJPP();
        }
        #endregion

        #region Extension Setup
        /// <summary>
        /// Init JPP command loads all essential elements of the program, including the helper DLL files.
        /// </summary>
        public static void InitJPP()
        {
            Logger.Log("Loading JPP Core...\n");
            //Create the main UI
            RibbonTab JPPTab = CreateTab();
            CreateCoreMenu(JPPTab);

            //Load the additional DLL files, but only not if running in debug mode
            #if !DEBUG
            Update();
            LoadModules();
            #endif

            //Create settings window
            //TODO: move common window creation code to utilities method
            _settingsWindow = new PaletteSet("JPP", new Guid("9dc86012-b4b2-49dd-81e2-ba3f84fdf7e3"));
            _settingsWindow.Size = new Size(600, 800);
            _settingsWindow.Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton + (int)PaletteSetStyles.ShowCloseButton);
            _settingsWindow.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);
                        
            ElementHost settingsWindowHost = new ElementHost();
            settingsWindowHost.AutoSize = true;
            settingsWindowHost.Dock = DockStyle.Fill;
            settingsWindowHost.Child = new SettingsUserControl();
            _settingsWindow.Add("Settings", settingsWindowHost);
            _settingsWindow.KeepFocus = false;

            //Load click handler;
            ClickOverride = ClickOverride.Current;

            //Check for registry key for autoload
            if (!RegistryHelper.IsAutoload())
            {
                //No autoload found
                //TODO: try to condense this into a helper method
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
            }

            Logger.Log("JPP Core loaded.\n");
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
            JPPTab.Name = Constants.Jpp_Tab_Title;
            JPPTab.Title = Constants.Jpp_Tab_Title;
            JPPTab.Id = Constants.Jpp_Tab_Id;

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
            source.Title = "General";

            RibbonRowPanel stack = new RibbonRowPanel();

            /*RibbonButton finaliseButton = Utilities.CreateButton("Finalise Drawing", Properties.Resources.package, RibbonItemSize.Standard, "Finalise");
            stack.Items.Add(finaliseButton);
            stack.Items.Add(new RibbonRowBreak());*/

            /*RibbonButton authenticateButton = Utilities.CreateButton("Authenticate", Properties.Resources.Locked, RibbonItemSize.Standard, "");
            stack.Items.Add(authenticateButton);
            stack.Items.Add(new RibbonRowBreak());*/

            //Create the button used to toggle the settings on or off
            _settingsButton = new RibbonToggleButton();//Utilities.CreateButton("Settings", Properties.Resources.settings, RibbonItemSize.Standard, "");            
            _settingsButton.ShowText = true;
            _settingsButton.ShowImage = true;
            _settingsButton.Text = "Settings";
            _settingsButton.Name = "Display the settings window";
            _settingsButton.CheckStateChanged += settingsButton_CheckStateChanged;
            _settingsButton.Image = Core.Utilities.LoadImage(Properties.Resources.settings);
            _settingsButton.Size = RibbonItemSize.Standard;
            _settingsButton.Orientation = System.Windows.Controls.Orientation.Horizontal;
            stack.Items.Add(_settingsButton);
            stack.Items.Add(new RibbonRowBreak());

            //Add the new tab section to the main tab
            source.Items.Add(stack);
            Panel.Source = source;
            JPPTab.Panels.Add(Panel);
        }        

        private static void settingsButton_CheckStateChanged(object sender, EventArgs e)
        {
            if(_settingsButton.CheckState == true)
            {
                _settingsWindow.Visible = true;
            } else
            {
                _settingsWindow.Visible = false;
            }
        }
        #endregion

        #region Updater
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
                        Assembly module = ExtensionLoader.Load(dll);
                    }
                }
            }
        }

        //TODO: Trigger update method somehow
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

        #endregion

        #region Command Methods

        [CommandMethod("Finalise", CommandFlags.Session)]
        public static void Finalise()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            using (DocumentLock dl = acDoc.LockDocument())
            {
                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {

                    //Run the cleanup commands
                    Core.Utilities.Purge();

                    acDoc.Database.Audit(true, false);
                }
            }

            string path = acDoc.Database.Filename;

            acDoc.Database.SaveAs(path, DwgVersion.Current);
            acDoc.CloseAndDiscard();

            FileInfo fi = new FileInfo(path);
            fi.IsReadOnly = true;
        }

        #endregion
    }
}
