﻿using System;
using System.Collections.Generic;
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
using JPP.Core;

namespace JPP.CivilStructures
{
    /// <summary>
    /// Interaction logic for SiteFoundationControl.xaml
    /// </summary>
    public partial class SiteFoundationControl : UserControl
    {
        public SiteFoundationControl()
        {
            InitializeComponent();

            PlasticitySelect.ItemsSource = Enum.GetValues(typeof(Shrinkage)).Cast<Shrinkage>();

            this.DataContext = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.GetDocumentStore<CivilStructureDocumentStore>().SiteFoundations;
        }
    }
}
