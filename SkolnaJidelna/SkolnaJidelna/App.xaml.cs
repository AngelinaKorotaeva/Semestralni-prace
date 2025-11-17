using System.Configuration;
using System.Data;
using System.Windows;
using Oracle.ManagedDataAccess.Client;

namespace SkolniJidelna;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        TestConn("User Id=ST69617;Password=ank11200;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=fei-sql3.upceucebny.cz)(PORT=1521))(CONNECT_DATA=(SID=BDAS)));");
    }

    void TestConn(string cs)
    {
        try
        {
            var conn = new OracleConnection(cs);
            conn.Open();
            MessageBox.Show("SUCCESS: " + cs);
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERROR: " + ex.Message + "\n\n" + cs);
        }
    }
}

