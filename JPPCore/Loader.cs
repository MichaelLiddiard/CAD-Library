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

namespace JPP.Core
{
    public class Loader : IExtensionApplication
    {
        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need
        /// </summary>
        public void Initialize()
        {
            InitJPP();
        }

        /// <summary>
        /// Implement the Autocad extension api to terminate the application
        /// </summary>
        public void Terminate()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("LoadJPP")]
        public static void Load()
        {
            List<string> allAssemblies = new List<string>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\bin";

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
            {
                string dllPath = dll.Replace('\\', '/');
                //Application.DocumentManager.MdiActiveDocument.SendStringToExecute("command \"NETLOAD\" \"" + dllPath + "\"", true, false, false);
                ResultBuffer args = new ResultBuffer(
                new TypedValue((int)LispDataType.Text, "command"),
                new TypedValue((int)LispDataType.Text, "NETLOAD"),
                new TypedValue((int)LispDataType.Text, dllPath));
                Application.Invoke(args);
                //Assembly loaded = Assembly.LoadFrom(dll);
            }            
        }

        [CommandMethod("Update")]
        public static void Update()
        {
            string archivePath = "M:\\ML\\CAD-Library\\Libraries-v";

            //Get manifest
            using (TextReader tr = File.OpenText("M:\\ML\\CAD-Library\\manifest.txt"))
            {
                archivePath = archivePath + tr.ReadToEnd() + ".zip";
            }

            //Download the latest DLL update
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
            //Add the menu options
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
            rc.Tabs.Add(JPPTab);
        }

    }
}
