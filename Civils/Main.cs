using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            RibbonRowPanel drainagePipeStack = new RibbonRowPanel();

            RibbonPanel utilitiesPanel = new RibbonPanel();
            RibbonPanelSource utilitiesSource = new RibbonPanelSource();
            RibbonRowPanel utilitiesStack = new RibbonRowPanel();
            RibbonRowPanel utilitiesStack2 = new RibbonRowPanel();

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
            drainagePipeStack.Items.Add(annotatePipeButton);

            utilitiesSource.Title = "Civil Utilities";

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
            utilitiesStack.Items.Add(new RibbonRowBreak());

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
            levelPLineButtone.Orientation = System.Windows.Controls.Orientation.Vertical;
            utilitiesStack2.Items.Add(levelPLineButtone);
            utilitiesStack2.Items.Add(new RibbonRowBreak());

            //Not sure why but something in the next three lines crashes the addin when auto loaded from init
            //Build the UI hierarchy
            source.Items.Add(drainagePipeStack);
            Panel.Source = source;

            utilitiesSource.Items.Add(utilitiesStack);
            utilitiesSource.Items.Add(utilitiesStack2);
            utilitiesPanel.Source = utilitiesSource;

            JPPTab.Panels.Add(Panel);
            JPPTab.Panels.Add(utilitiesPanel);
        }

        public static void LoadBlocks()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (Database OpenDb = new Database(false, true))
            {               
                string path = Assembly.GetExecutingAssembly().Location;
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
    }
}
