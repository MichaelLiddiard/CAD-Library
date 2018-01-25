using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Internal.Windows;
using Autodesk.Windows;
using JPP.Core;
using JPPCommands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;

[assembly: ExtensionApplication(typeof(JPP.Civils.Main))]
[assembly: CommandClass(typeof(JPP.Civils.Main))]

namespace JPP.Civils
{
    public class Main : IExtensionApplication
    {
        PaletteSet _ps;
        PlotUserControl uc2;
        PlotTypeUserControl uc3;

        RibbonToggleButton plotButton;

        public static bool C3DActive;

        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need
        /// </summary>
        public void Initialize()
        {
            //Add the menu options
            RibbonControl rc = Autodesk.Windows.ComponentManager.Ribbon;
            RibbonTab JPPTab = rc.FindTab("JPPCORE_JPP_TAB");
            if (JPPTab == null)
            {
                JPPTab = JPP.Core.JPPMain.CreateTab();
            }

            RibbonPanel Panel = new RibbonPanel();
            RibbonPanelSource source = new RibbonPanelSource();
            RibbonRowPanel drainagePipeStack = new RibbonRowPanel();

            RibbonPanel utilitiesPanel = new RibbonPanel();
            RibbonPanelSource utilitiesSource = new RibbonPanelSource();
            RibbonRowPanel plotStack = new RibbonRowPanel();
            RibbonRowPanel utilitiesStack = new RibbonRowPanel();
            RibbonRowPanel utilitiesStack2 = new RibbonRowPanel();

            RibbonPanel fflPanel = new RibbonPanel();
            RibbonPanelSource fflSource = new RibbonPanelSource();
            RibbonRowPanel fflStack = new RibbonRowPanel();

            source.Title = "Civil Drainage";

            //Add button to re load all JPP libraries
            RibbonButton layPipeButton = new RibbonButton();
            layPipeButton.ShowText = true;
            layPipeButton.ShowImage = true;
            layPipeButton.Text = "Lay Pipe";
            layPipeButton.Name = "Lay Pipe";
            layPipeButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            layPipeButton.CommandParameter = "._LayPipe ";
            layPipeButton.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.pipeIcon);
            layPipeButton.Image = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.pipeIcon_small);
            layPipeButton.Size = RibbonItemSize.Standard;
            layPipeButton.IsEnabled = false;
            drainagePipeStack.Items.Add(layPipeButton);
            drainagePipeStack.Items.Add(new RibbonRowBreak());

            //Add button to re load all JPP libraries
            RibbonButton annotatePipeButton = new RibbonButton();
            annotatePipeButton.ShowText = true;
            annotatePipeButton.Text = "Annotate Pipe";
            annotatePipeButton.Name = "Annotate Pipe";
            annotatePipeButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            annotatePipeButton.CommandParameter = "._AnnotatePipe ";
            annotatePipeButton.Image = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.pipeAnnotate_small);
            annotatePipeButton.Size = RibbonItemSize.Standard;
            annotatePipeButton.IsEnabled = false;
            drainagePipeStack.Items.Add(annotatePipeButton);

            utilitiesSource.Title = "Civil Utilities";

            //Add button to import xref
            plotButton = new RibbonToggleButton();
            //RibbonButton plotButton = new RibbonButton();
            plotButton.ShowText = true;
            plotButton.ShowImage = true;
            plotButton.Text = "Housing Scheme";
            plotButton.Name = "Import As Xref";
            plotButton.CheckStateChanged += PlotButton_CheckStateChanged;
            plotButton.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.housingscheme);
            plotButton.Size = RibbonItemSize.Large;
            plotButton.Orientation = System.Windows.Controls.Orientation.Vertical;
            plotStack.Items.Add(plotButton);

            //Add button to import xref
            RibbonButton importXrefButton = new RibbonButton();
            importXrefButton.ShowText = true;
            importXrefButton.ShowImage = true;
            importXrefButton.Text = "Import As Xref";
            importXrefButton.Name = "Import As Xref";
            importXrefButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            importXrefButton.CommandParameter = "._ImportAsXref ";
            importXrefButton.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.importXref);
            importXrefButton.Size = RibbonItemSize.Large;
            importXrefButton.Orientation = System.Windows.Controls.Orientation.Vertical;
            utilitiesStack.Items.Add(importXrefButton);

            //Add button to level polyline
            RibbonButton levelPLineButtone = new RibbonButton();
            levelPLineButtone.ShowText = true;
            levelPLineButtone.ShowImage = true;
            levelPLineButtone.Text = "Level Polyline";
            levelPLineButtone.Name = "Level Polyline";
            levelPLineButtone.CommandHandler = new JPP.Core.RibbonCommandHandler();
            levelPLineButtone.CommandParameter = "._LevelPolyline ";
            levelPLineButtone.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.importXref);
            levelPLineButtone.Size = RibbonItemSize.Standard;
            levelPLineButtone.IsEnabled = false;
            levelPLineButtone.Orientation = System.Windows.Controls.Orientation.Vertical;
            utilitiesStack2.Items.Add(levelPLineButtone);
            utilitiesStack2.Items.Add(new RibbonRowBreak());

            /*fflSource.Title = "Plot Commands";
            //Add button to import xref
            RibbonButton addFFLButton = new RibbonButton();
            addFFLButton.ShowText = true;
            addFFLButton.ShowImage = true;
            addFFLButton.Text = "Create Plot";
            addFFLButton.Name = "Create Plot";
            addFFLButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            addFFLButton.CommandParameter = "._NewFFL ";
            //addFFLButton.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.importXref);
            addFFLButton.Size = RibbonItemSize.Standard;
            fflStack.Items.Add(addFFLButton);
            fflStack.Items.Add(new RibbonRowBreak());

            //Add button to import xref
            RibbonButton editFFLButton = new RibbonButton();
            editFFLButton.ShowText = true;
            editFFLButton.ShowImage = true;
            editFFLButton.Text = "Edit Plot";
            editFFLButton.Name = "Edit Plot";
            editFFLButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            editFFLButton.CommandParameter = "._EditFFL ";
            //editFFLButton.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.importXref);
            editFFLButton.Size = RibbonItemSize.Standard;
            fflStack.Items.Add(editFFLButton);
            fflStack.Items.Add(new RibbonRowBreak());

            //Add button to import xref
            RibbonButton plineToFFLButton = new RibbonButton();
            plineToFFLButton.ShowText = true;
            plineToFFLButton.ShowImage = true;
            plineToFFLButton.Text = "FFL From PLine";
            plineToFFLButton.Name = "FFL From PLine";
            plineToFFLButton.CommandHandler = new JPP.Core.RibbonCommandHandler();
            plineToFFLButton.CommandParameter = "._PlineToFFL ";
            //plineToFFLButton.LargeImage = Core.Utilities.LoadImage(JPP.Civils.Properties.Resources.importXref);
            plineToFFLButton.Size = RibbonItemSize.Standard;
            fflStack.Items.Add(plineToFFLButton);*/

            //Build the UI hierarchy
            source.Items.Add(drainagePipeStack);
            Panel.Source = source;

            utilitiesSource.Items.Add(plotStack);
            utilitiesSource.Items.Add(utilitiesStack);
            utilitiesSource.Items.Add(utilitiesStack2);
            utilitiesPanel.Source = utilitiesSource;

            fflSource.Items.Add(fflStack);
            fflPanel.Source = fflSource;

            JPPTab.Panels.Add(Panel);
            JPPTab.Panels.Add(utilitiesPanel);
            //JPPTab.Panels.Add(fflPanel);

            _ps = new PaletteSet("JPP", new Guid("8bc0c89e-3be0-4e30-975e-1a4e09cb0524"));
            _ps.Size = new Size(600, 800);
            _ps.Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton + (int)PaletteSetStyles.ShowCloseButton);
            _ps.DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right);

            /*SettingsUserControl suc = new SettingsUserControl();
            ElementHost host1 = new ElementHost();
            host1.AutoSize = true;
            host1.Dock = DockStyle.Fill;
            host1.Child = suc;
            _ps.Add("Settings", host1);*/

            uc2 = new PlotUserControl();
            ElementHost host2 = new ElementHost();
            host2.AutoSize = true;
            host2.Dock = DockStyle.Fill;
            host2.Child = uc2;
            uc3 = new PlotTypeUserControl();
            ElementHost host3 = new ElementHost();
            host3.AutoSize = true;
            host3.Dock = DockStyle.Fill;
            host3.Child = uc3;
            _ps.Add("Plot Types", host3);
            _ps.Add("Plots", host2);

            // Display our palette set

            _ps.KeepFocus = false;

            //Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.DocumentActivationChanged += DocumentManager_DocumentActivationChanged;
            Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.DocumentActivated += PlotButton_CheckStateChanged;


            //Throws exception
            //JPPCommandsInitialisation.JPPCommandsInitialise();

            //Check if running under Civil3D by trying to load dll
            try
            {
                AttemptC3DLoad();
                C3DActive = true;
            }
            catch (System.Exception e)
            {
                C3DActive = false;
            }

            //Added registered simble for XData
            AddRegAppTableRecord();
        }

        private static void AttemptC3DLoad()
        {
            CivilDocument currentC3D = CivilApplication.ActiveDocument;

        }

        private void DocumentManager_DocumentActivationChanged(object sender, DocumentActivationChangedEventArgs e)
        {
            _ps.Visible = false;
            uc2.DataContext = null;
            uc3.DataContext = null;
        }

        private void PlotButton_CheckStateChanged(object sender, EventArgs e)
        {
            //if(((RibbonToggleButton)sender).CheckState == true)
            if ((plotButton).CheckState == true)
            {
                if (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument != null)
                {
                    uc2.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().Plots;
                    uc3.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilDocumentStore>().PlotTypes;
                }
                _ps.Visible = true;
            }
            else
            {
                _ps.Visible = false;
                uc2.DataContext = null;
                uc3.DataContext = null;
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

        static void AddRegAppTableRecord()
        {

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Database db = doc.Database;

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
    }
}
