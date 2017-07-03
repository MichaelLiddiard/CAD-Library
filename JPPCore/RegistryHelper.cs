using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    class RegistryHelper
    {
        public static bool IsAutoload()
        {
            RegistryKey hive = Registry.CurrentUser;

            // Open the main AutoCAD (or vertical) and "Applications" keys            
            using (RegistryKey ack = hive.OpenSubKey(HostApplicationServices.Current.UserRegistryProductRootKey, true))
            {
                using (RegistryKey app = ack.OpenSubKey("Applications"))
                {
                    //Get list of registered apps
                    string[] apps = app.GetSubKeyNames();
                    foreach (string s in apps)
                    {
                        if (s == Constants.Registry_Name)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void CreateAutoload()
        {
            RegistryKey hive = Registry.CurrentUser;

            // Open the main AutoCAD (or vertical) and "Applications" keys            
            using (RegistryKey ack = hive.OpenSubKey(HostApplicationServices.Current.UserRegistryProductRootKey, true))
            {
                using (RegistryKey app = ack.CreateSubKey("Applications"))
                {
                    //Get list of registered apps
                    string[] apps = app.GetSubKeyNames();
                    bool found = false;
                    foreach (string s in apps)
                    {
                        if (s == Constants.Registry_Name)
                        {
                            found = true;
                        }
                    }

                    using (RegistryKey rKey = app.CreateSubKey(Constants.Registry_Name))
                    {
                        rKey.SetValue("DESCRIPTION", "Loader for JPP Cad Library", Microsoft.Win32.RegistryValueKind.String);
                        rKey.SetValue("LOADCTRLS", 2, Microsoft.Win32.RegistryValueKind.DWord);
                        rKey.SetValue("LOADER", Assembly.GetExecutingAssembly().Location, Microsoft.Win32.RegistryValueKind.String);
                        rKey.SetValue("MANAGED", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
        }/*

                RegistryKey appk = ack.CreateSubKey(Constants.Registry_Name);
                using (appk)
                {
                    // Already registered? Just return
                    string[] subKeys = appk.GetSubKeyNames();
                    foreach (string subKey in subKeys)
                    {
                        if (subKey.Equals(name))
                        {
                            return;
                        }
                    }

                    // Create the our application's root key and its values

                    RegistryKey rk = appk.CreateSubKey(name);
                    using (rk)
                    {
                        rk.SetValue(
                          "DESCRIPTION", name, RegistryValueKind.String
                        );
                        rk.SetValue("LOADCTRLS", flags, RegistryValueKind.DWord);
                        rk.SetValue("LOADER", path, RegistryValueKind.String);
                        rk.SetValue("MANAGED", 1, RegistryValueKind.DWord);

                        // Create a subkey if there are any commands...

                        if ((globCmds.Count == locCmds.Count) &&
                             globCmds.Count > 0)
                        {
                            RegistryKey ck = rk.CreateSubKey("Commands");
                            using (ck)
                            {
                                for (int i = 0; i < globCmds.Count; i++)
                                    ck.SetValue(
                                      globCmds[i],
                                      locCmds[i],
                                      RegistryValueKind.String
                                    );
                            }
                        }

                        // And the command groups, if there are any

                        if (groups.Count > 0)
                        {
                            RegistryKey gk = rk.CreateSubKey("Groups");
                            using (gk)
                            {
                                foreach (string grpName in groups)
                                    gk.SetValue(
                                      grpName, grpName, RegistryValueKind.String
                                    );
                            }
                        }
                    }
                }
            }

            return false;
        }*/
    }
}
