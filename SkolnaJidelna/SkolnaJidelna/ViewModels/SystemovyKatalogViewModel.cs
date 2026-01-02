using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using SkolniJidelna.Data;
using SkolniJidelna.Helpers;

namespace SkolniJidelna.ViewModels
{
    /// <summary>
    /// ViewModel pro okno Systemového katalogu.
    /// Poskytuje seznam dotazů (položky do ComboBoxu), udržuje vybraný index
    /// a na základě něj asynchronně načítá výsledky z databáze přes DbContext.
    /// Výsledky jsou vystaveny jako ObservableCollection pro snadnou vazbu na UI.
    /// </summary>
    public class SystemovyKatalogViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Přehled dostupných dotazů, které se zobrazují v ComboBoxu.
        /// Po změně výběru se spustí odpovídající SQL dotaz.
        /// </summary>
        public IList<string> Queries { get; } = new List<string>
        {
            "1) Názvy tabulek",
            "2) Povolení NULL hodnot (STRAVNICI)",
            "3) Primární klíče",
            "4) Cizí klíče",
            "5) Indexy",
            "6) Pohledy",
            "7) Triggery",
            "8) Procedury",
            "9) Funkce",
            "10) Sekvence"
        };

        private int _selectedIndex = -1;
        /// <summary>
        /// Index aktuálně vybraného dotazu. Změna vyvolá načtení nových výsledků.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
                _ = LoadResultsAsync();
            }
        }

        /// <summary>
        /// Kolekce výsledků posledního provedeného dotazu. Vhodná pro přímou vazbu na ListBox.
        /// </summary>
        public ObservableCollection<string> Results { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Příkaz pro ruční obnovení výsledků aktuálně vybraného dotazu.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Vytvoření viewmodelu s předaným DbContextem. Připraví příkaz Refresh.
        /// </summary>
        public SystemovyKatalogViewModel(AppDbContext db)
        {
            _db = db;
            RefreshCommand = new RelayCommand(async _ => await LoadResultsAsync(), _ => SelectedIndex >= 0);
        }

        /// <summary>
        /// Asynchronně provede SQL dotaz dle vybraného indexu a naplní kolekci Results.
        /// </summary>
        public async Task LoadResultsAsync()
        {
            if (SelectedIndex < 0) return;
            try
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();

                string sql = GetSqlByIndex(SelectedIndex);
                using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                if (cmd is OracleCommand ocmd) ocmd.BindByName = true;
                cmd.CommandText = sql;
                var list = new List<string>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var parts = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (reader.IsDBNull(i)) { parts[i] = string.Empty; continue; }
                        var ft = reader.GetFieldType(i);
                        try
                        {
                            if (ft == typeof(string))
                            {
                                parts[i] = reader.GetString(i);
                            }
                            else if (ft == typeof(int))
                            {
                                parts[i] = reader.GetInt32(i).ToString();
                            }
                            else if (ft == typeof(long))
                            {
                                parts[i] = reader.GetInt64(i).ToString();
                            }
                            else if (ft == typeof(decimal))
                            {
                                parts[i] = reader.GetDecimal(i).ToString();
                            }
                            else if (ft == typeof(DateTime))
                            {
                                parts[i] = reader.GetDateTime(i).ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                var val = reader.GetValue(i);
                                if (val is byte[] bytes)
                                {
                                    // Pokus o převod binárních dat na text (pro RAW/BLOB případy)
                                    string s;
                                    try { s = System.Text.Encoding.UTF8.GetString(bytes); }
                                    catch { s = System.Text.Encoding.Default.GetString(bytes); }
                                    parts[i] = s;
                                }
                                else
                                {
                                    parts[i] = val?.ToString() ?? string.Empty;
                                }
                            }
                        }
                        catch
                        {
                            parts[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                        }
                    }
                    list.Add(string.Join(" | ", parts));
                }
                // Populate Results (previously cleared only)
                Results.Clear();
                foreach (var item in list)
                {
                    Results.Add(item);
                }
            }
            catch (Exception ex)
            {
                // Optional: show error in results
                Results.Clear();
                Results.Add("Chyba: " + ex.Message);
            }
            finally
            {
                try { var c = _db.Database.GetDbConnection(); if (c.State == ConnectionState.Open) c.Close(); } catch { }
            }
        }

        /// <summary>
        /// Vrátí SQL dotaz podle vybraného indexu položky v ComboBoxu.
        /// Některé dotazy filtrují recyklované objekty Oracle (BIN$...).
        /// </summary>
        private static string GetSqlByIndex(int index)
        {
            return index switch
            {
                0 => "SELECT table_name FROM user_tables",
                1 => "SELECT column_name, nullable FROM user_tab_columns WHERE table_name = 'STRAVNICI'",
                2 => "SELECT constraint_name, table_name FROM user_constraints WHERE constraint_type = 'P' AND constraint_name NOT LIKE 'BIN$%'",
                3 => "SELECT constraint_name, table_name FROM user_constraints WHERE constraint_type = 'R' AND constraint_name NOT LIKE 'BIN$%'",
                4 => "SELECT index_name, table_name, uniqueness FROM user_indexes",
                5 => "SELECT view_name FROM user_views",
                6 => "SELECT trigger_name, table_name, triggering_event FROM user_triggers",
                7 => "SELECT object_name FROM user_objects WHERE object_type = 'PROCEDURE'",
                8 => "SELECT object_name FROM user_objects WHERE object_type = 'FUNCTION'",
                9 => "SELECT sequence_name FROM user_sequences",
                _ => "SELECT 'Vyberte dotaz'"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
