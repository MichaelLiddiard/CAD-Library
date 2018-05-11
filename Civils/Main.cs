using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Windows;
using JPP.Core;
using JPPCommands;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(JPP.Civils.Main))]
[assembly: CommandClass(typeof(JPP.Civils.Main))]

namespace JPP.Civils
{
    public class Main : IExtensionApplication
    {
        PaletteSet _ps;
        PlotUserControl _uc2;
        PlotTypeUserControl _uc3;

        //TODO: Review accessability of this
        public static Library<PlotType> PtLibrary;

        RibbonToggleButton _plotButton;

        public static bool C3DActive;        

        /// <summary>
        /// Implement the Autocad extension api to load the core elements of the civil api
        /// </summary>
        public void Initialize()
        {
            Logger.Log("Loading Civil module...");
            PtLibrary = new Library<PlotType>("M:\\ML\\CAD-Library\\Library\\PlotTypes");

            //Check if running under Civil3D by trying to load dll
            try
            {
                AttemptC3DLoad();
                C3DActive = true;
            }
            catch (System.Exception)
            {
                C3DActive = false;
            }
            Logger.Log("Civil 3d checked\n", Logger.Severity.Debug);

            //Add the menu options
            RibbonControl rc = ComponentManager.Ribbon;
            RibbonTab jppTab = rc.FindTab(Core.Constants.Jpp_Tab_Id);
            if (jppTab == null)
            {
                Logger.Log("No tabs has been created by core\n", Logger.Severity.Warning);
                jppTab = JPPMain.CreateTab();
            }

            //Create Drainage UI
            RibbonPanel drainagePanel = new RibbonPanel();
            RibbonPanelSource drainagePanelSource = new RibbonPanelSource();
            RibbonRowPanel drainageVerticalStack = new RibbonRowPanel();
            drainagePanelSource.Title = "Civil Drainage";
            
            RibbonButton layPipeButton = Core.Utilities.CreateButton("Lay Pipe", Properties.Resources.pipeIcon_small, RibbonItemSize.Standard, "._LayPipe");
            layPipeButton.IsEnabled = false;
            drainageVerticalStack.Items.Add(layPipeButton);
            drainageVerticalStack.Items.Add(new RibbonRowBreak());

            RibbonButton annotatePipeButton = Core.Utilities.CreateButton("Annotate Pipe", Properties.Resources.pipeAnnotate_small, RibbonItemSize.Standard, "._AnnotatePipe");
            annotatePipeButton.IsEnabled = false;
            drainageVerticalStack.Items.Add(annotatePipeButton);
            
            drainagePanelSource.Items.Add(drainageVerticalStack);
            drainagePanel.Source = drainagePanelSource;
            jppTab.Panels.Add(drainagePanel);

            //Create the Utilities UI
            RibbonPanel utilitiesPanel = new RibbonPanel();
            RibbonPanelSource utilitiesPanelSource = new RibbonPanelSource();
            RibbonRowPanel plotStack = new RibbonRowPanel();
            RibbonRowPanel utilitiesStack = new RibbonRowPanel();
            RibbonRowPanel utilitiesStack2 = new RibbonRowPanel();

            utilitiesPanelSource.Title = "Civil Utilities";

            //Add button to import xref
            _plotButton = new RibbonToggleButton
            {
                //RibbonButton plotButton = new RibbonButton();
                ShowText = true,
                ShowImage = true,
                Text = "Housing Scheme",
                Name = "Import As Xref"
            };
            _plotButton.CheckStateChanged += PlotButton_CheckStateChanged;
            _plotButton.LargeImage = Core.Utilities.LoadImage(Properties.Resources.housingscheme);
            _plotButton.Size = RibbonItemSize.Large;
            _plotButton.Orientation = System.Windows.Controls.Orientation.Vertical;
            plotStack.Items.Add(_plotButton);
            SetHousingSchemeRequirements();

            //Add button to import xref
            RibbonButton importXrefButton = Core.Utilities.CreateButton("Import as Xref", Properties.Resources.importXref, RibbonItemSize.Large, "._ImportAsXref");
            importXrefButton.Orientation = System.Windows.Controls.Orientation.Vertical;
            utilitiesStack.Items.Add(importXrefButton);

            //Add button to level polyline
            RibbonButton levelPLineButtone = Core.Utilities.CreateButton("Level Polyline", Properties.Resources.importXref, RibbonItemSize.Standard, "._LevelPolyline");
            levelPLineButtone.IsEnabled = false;
            levelPLineButtone.Orientation = System.Windows.Controls.Orientation.Vertical;
            utilitiesStack2.Items.Add(levelPLineButtone);
            utilitiesStack2.Items.Add(new RibbonRowBreak());

            utilitiesPanelSource.Items.Add(plotStack);
            utilitiesPanelSource.Items.Add(utilitiesStack);
            utilitiesPanelSource.Items.Add(utilitiesStack2);
            utilitiesPanel.Source = utilitiesPanelSource;
            
            jppTab.Panels.Add(utilitiesPanel);
            Logger.Log("UI created\n", Logger.Severity.Debug);


            _ps = new PaletteSet("JPP", new Guid("8bc0c89e-3be0-4e30-975e-1a4e09cb0524"))
            {
                Size = new Size(600, 800),
                Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton + (int)PaletteSetStyles.ShowCloseButton),
                DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right)
            };

            _uc2 = new PlotUserControl();
            ElementHost host2 = new ElementHost
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Child = _uc2
            };
            _uc3 = new PlotTypeUserControl();
            ElementHost host3 = new ElementHost
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Child = _uc3
            };
            _ps.Add("Plot Types", host3);
            _ps.Add("Plots", host2);

            // Display our palette set
            _ps.KeepFocus = false;

            //Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.DocumentActivationChanged += DocumentManager_DocumentActivationChanged;
            Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.DocumentActivated += PlotButton_CheckStateChanged;
            Logger.Log("Dock created\n", Logger.Severity.Debug);
            
            Application.Idle += Application_Idle;

            //Load click overrides
            //TODO: Fix and re-enable
            //ClickOverride.Current.Add(new LevelClickHandler());
            Logger.Log("Civil module loaded.\n");
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;
            //Added registered simble for XData
            AddRegAppTableRecord();
            Logger.Log("Registered civil symbols\n", Logger.Severity.Debug);
        }

        /// <summary>
        /// Create required layers and surfaces
        /// </summary>
        private void SetHousingSchemeRequirements()
        {
            
        }

        private static void AttemptC3DLoad()
        {
            CivilDocument currentC3D = CivilApplication.ActiveDocument;

        }

        private void DocumentManager_DocumentActivationChanged(object sender, DocumentActivationChangedEventArgs e)
        {
            _ps.Visible = false;
            _uc2.DataContext = null;
            _uc3.DataContext = null;
        }

        private void PlotButton_CheckStateChanged(object sender, EventArgs e)
        {
            //if(((RibbonToggleButton)sender).CheckState == true)
            if ((_plotButton).CheckState == true)
            {
                if (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument != null)
                {
                    _uc2.dataGrid.ItemsSource = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().Plots;
                    _uc3.plotTypeGrid.ItemsSource = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().PlotTypes;
                }
                _ps.Visible = true;
            }
            else
            {
                _ps.Visible = false;
                _uc2.DataContext = null;
                _uc3.DataContext = null;
            }
        }

        public static void LoadBlocks()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            using (Database OpenDb = new Database(false, true))
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                path = path.Replace("Civils.dll", "");
                doc.Editor.WriteMessage(path);
                OpenDb.ReadDwgFile(path + "Templates.dwg", System.IO.FileShare.ReadWrite, true, "");

                ObjectIdCollection ids = new ObjectIdCollection();
                using (Transaction tr = OpenDb.TransactionManager.StartTransaction())
                {
                    //For example, Get the block by name "TEST"
                    BlockTable bt;
                    bt = (BlockTable)tr.GetObject(OpenDb.BlockTableId, OpenMode.ForRead);

                    if (bt.Has("PipeLabel"))
                    {
                        ids.Add(bt["PipeLabel"]);
                    }
                    if (bt.Has("FoulAdoptableManhole"))
                    {
                        ids.Add(bt["FoulAdoptableManhole"]);
                    }
                    if (bt.Has("StormAdoptableManhole"))
                    {
                        ids.Add(bt["StormAdoptableManhole"]);
                    }
                    if (bt.Has("ProposedLevel"))
                    {
                        ids.Add(bt["ProposedLevel"]);
                    }
                    tr.Commit();
                }

                //if found, add the block
                if (ids.Count != 0)
                {
                    //get the current drawing database
                    Database destdb = doc.Database;

                    IdMapping iMap = new IdMapping();
                    destdb.WblockCloneObjects(ids, destdb.BlockTableId, iMap, DuplicateRecordCloning.Ignore, false);
                }
            }
        }

        /// <summary>
        /// Implement the Autocad extension api to terminate the application
        /// </summary>
        public void Terminate()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("NewFFL")]
        public static void NewFFL()
        {
            JPPCommandsInitialisation.JPPCommandsInitialise();
            AddFFL.NewFFL();
        }

        [CommandMethod("EditFFL")]
        public static void EditFFL()
        {
            JPPCommandsInitialisation.JPPCommandsInitialise();
            JPPCommands.EditFFL.EditFFLOrLevels();
        }

        [CommandMethod("DeleteFFL")]
        public static void DeleteFFL()
        {
            JPPCommandsInitialisation.JPPCommandsInitialise();
            throw new NotImplementedException();//JPPCommands.
        }

        public static void AddRegAppTableRecord()
        {
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;

                Database db = doc.Database;

                using (var Lock = doc.LockDocument())
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {

                        RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead, false);

                        if (!rat.Has("JPP"))
                        {
                            rat.UpgradeOpen();
                            RegAppTableRecord ratr = new RegAppTableRecord();
                            ratr.Name = "JPP";
                            rat.Add(ratr);
                            tr.AddNewlyCreatedDBObject(ratr, true);
                        }
                        tr.Commit();
                    }
                }
            } catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                Logger.Log("Error in registering AppTableRecord - " + e.Message + "\n");
            }
            catch (System.Exception e)
            {
                Logger.Log("Error in registering AppTableRecord - " + e.Message + "\n");
            }
        }
    }
}
