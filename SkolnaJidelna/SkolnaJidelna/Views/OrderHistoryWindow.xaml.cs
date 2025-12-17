using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using SkolniJidelna.Services;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    public partial class OrderHistoryWindow : Window
    {
        private string Email;
        private OrderHistoryViewModel _vm;
        public OrderHistoryWindow(string email)
        {
            InitializeComponent();
            this.Email = email;
            _vm = new OrderHistoryViewModel(email);
            this.DataContext = _vm;

            _vm.SelectedStav = "Vše";
            _vm.LoadOrders();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (comboStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                _vm.SetFilter(selected);
            }
        }

        private void OrderSelected(object sender, SelectionChangedEventArgs e)
        {
            // VM handles SelectedOrder via binding
        }

        private void PayCardButton_Click(object sender, RoutedEventArgs e)
        {
            var order = _vm.SelectedOrder;
            if (order == null) return;
            var pw = new PaymentWindow(PaymentWindow.PaymentMethod.Card) { Owner = this };
            if (pw.ShowDialog() == true)
            {
                try
                {
                    _vm.PayOrder(order, OrderHistoryViewModel.PaymentMethod.Card);
                    MessageBox.Show("Platba proběhla.");
                    _vm.LoadOrders();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Chyba platby: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PayAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var order = _vm.SelectedOrder;
            if (order == null) return;
            var pw = new PaymentWindow(PaymentWindow.PaymentMethod.Account) { Owner = this };
            if (pw.ShowDialog() == true)
            {
                try
                {
                    _vm.PayOrder(order, OrderHistoryViewModel.PaymentMethod.Account);
                    MessageBox.Show("Platba proběhla.");
                    _vm.LoadOrders();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Chyba platby: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var order = _vm.SelectedOrder;
            if (order == null) return;
            if (MessageBox.Show("Opravdu chcete zrušit objednávku?", "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                _vm.CancelOrder(order);
                MessageBox.Show("Objednávka byla zrušena.");
                _vm.LoadOrders();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba při rušení objednávky: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin = false;
            bool isPracovnik = false;
            try
            {
                using var ctx = new AppDbContext();
                var v = ctx.VStravnikLogin.AsNoTracking().FirstOrDefault(x => x.Email == Email);
                var role = v?.Role?.Trim();
                var type = v?.TypStravnik?.Trim();
                isAdmin = string.Equals(role, "ADMIN", System.StringComparison.OrdinalIgnoreCase);
                isPracovnik = string.Equals(type, "pr", System.StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
            }

            try
            {
                var svc = App.Services.GetService(typeof(IWindowService)) as IWindowService;
                if (svc != null)
                {
                    if (isAdmin)
                        svc.ShowAdminProfile(Email);
                    else
                        svc.ShowUserProfile(Email, isPracovnik);
                }
                else
                {
                    if (isAdmin)
                    {
                        var aw = new AdminProfileWindow(Email);
                        aw.Show();
                    }
                    else
                    {
                        var up = new UserProfileWindow(Email);
                        up.Show();
                    }
                }
            }
            catch
            {
                if (isAdmin)
                {
                    var aw = new AdminProfileWindow(Email);
                    aw.Show();
                }
                else
                {
                    var up = new UserProfileWindow(Email);
                    up.Show();
                }
            }

            this.Close();
        }

        private void PrintPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var order = _vm.SelectedOrder;
            if (order == null)
            {
                MessageBox.Show("Vyberte objednávku pro tisk.");
                return;
            }

            // Show print dialog; user saves as PDF via PDF printer
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;

            // Build FlowDocument (same content as before) to print
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12
            };
            doc.Blocks.Add(new Paragraph(new Run("Objednávka #" + order.IdObjednavka)) { FontSize = 18, FontWeight = System.Windows.FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run($"Datum odběru: {order.DatumOdberu:dd.MM.yyyy}")));
            doc.Blocks.Add(new Paragraph(new Run($"Vytvořeno: {(order.DatumVytvoreni.HasValue ? order.DatumVytvoreni.Value.ToString("dd.MM.yyyy HH:mm") : "-")}")));
            doc.Blocks.Add(new Paragraph(new Run($"Stav: {order.StavNazev}")));
            doc.Blocks.Add(new Paragraph(new Run($"Celková cena: {order.CelkovaCena:0.##} Kč")) { FontWeight = System.Windows.FontWeights.Bold });
            if (!string.IsNullOrWhiteSpace(order.Poznamka))
                doc.Blocks.Add(new Paragraph(new Run($"Poznámka: {order.Poznamka}")));
            var table = new Table();
            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn());
            var header = new TableRow();
            header.Cells.Add(new TableCell(new Paragraph(new Run("Jídlo"))) { FontWeight = System.Windows.FontWeights.Bold });
            header.Cells.Add(new TableCell(new Paragraph(new Run("Počet"))) { FontWeight = System.Windows.FontWeights.Bold });
            header.Cells.Add(new TableCell(new Paragraph(new Run("Cena"))) { FontWeight = System.Windows.FontWeights.Bold });
            var rg = new TableRowGroup();
            rg.Rows.Add(header);
            foreach (var it in order.Items)
            {
                var r = new TableRow();
                r.Cells.Add(new TableCell(new Paragraph(new Run(it.Nazev))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(it.Mnozstvi.ToString()))));
                r.Cells.Add(new TableCell(new Paragraph(new Run(it.Cena.ToString("0.##") + " Kč"))));
                rg.Rows.Add(r);
            }
            table.RowGroups.Add(rg);
            doc.Blocks.Add(table);

            IDocumentPaginatorSource idp = doc;
            dlg.PrintDocument(idp.DocumentPaginator, $"Objednavka_{order.IdObjednavka}");

            // Ask user to pick the saved PDF and persist to DB
            var ofd = new OpenFileDialog
            {
                Title = "Vyberte uložený PDF soubor",
                Filter = "PDF soubory (*.pdf)|*.pdf",
                CheckFileExists = true
            };
            if (ofd.ShowDialog(this) == true)
            {
                try
                {
                    var bytes = File.ReadAllBytes(ofd.FileName);
                    _vm.SavePdfFile(order, ofd.FileName, bytes);

                    // Open the PDF file for viewing
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = ofd.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch { }

                    MessageBox.Show("PDF bylo uloženo do Soubory.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba při ukládání PDF: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}