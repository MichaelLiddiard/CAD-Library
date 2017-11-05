using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JPP.Core
{
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {
        ObservableCollection<SettingsKeyValue> settingsList;

        public SettingsUserControl()
        {           

            settingsList = new ObservableCollection<SettingsKeyValue>();

            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                SettingsKeyValue skv = new SettingsKeyValue() { Properties = currentProperty.Name, Value = currentProperty.DefaultValue.ToString() };
                settingsList.Add(skv);
            }            

            InitializeComponent();

            settingsGrid.ItemsSource = settingsList;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            foreach (SettingsKeyValue skv in settingsList)
            {
                Properties.Settings.Default.Properties[skv.Properties].DefaultValue = skv.Value;
                Properties.Settings.Default.ModulePath = skv.Value;
            }

            Properties.Settings.Default.Save();
        }

        /*Properties.Settings.Default[currentProperty.Name] = result.ToString();
                Properties.Settings.Default.Save();*/


    }

    class SettingsKeyValue
    {
        public string Properties { get; set; }
        public string Value { get; set; }
    }

}
