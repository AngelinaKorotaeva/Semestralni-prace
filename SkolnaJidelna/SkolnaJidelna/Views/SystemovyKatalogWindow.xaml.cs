using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Data;

namespace SkolniJidelna
{
    public partial class SystemovyKatalogWindow : Window
    {
        private readonly AppDbContext _db;
        public SystemovyKatalogWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            this.DataContext = new SkolniJidelna.ViewModels.SystemovyKatalogViewModel(db);
        }

        private void QuerySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is SkolniJidelna.ViewModels.SystemovyKatalogViewModel vm)
            {
                vm.SelectedIndex = QuerySelector.SelectedIndex;
                ResultList.ItemsSource = vm.Results;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
