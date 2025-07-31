using System;
using System.Collections.Generic;
using System.Data;
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
using ControlzEx.Theming;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualBasic;


namespace WpfApp2.CircuitsList
{
    /// <summary>
    /// Interaction logic for circuitsList.xaml
    /// </summary>
    public partial class circuitsList : UserControl
    {
        DatabaseAccess acessos = new();
        public circuitsList()
        {
            InitializeComponent();
            MySnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
            findProjects();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string elementoDeBusca = txtSearchMain.Text;

            string varSql = $"SELECT id, TAG, VOLT, \"FROM\", \"TO\", REMARKS FROM cableList_modeste WHERE (";

            if (chkTag.IsChecked == true)
                varSql = varSql + $"TAG LIKE '%{elementoDeBusca}%' OR ";
            if (chkTag.IsChecked == true)
                varSql = varSql + $"id LIKE '%{elementoDeBusca}%' OR ";
            if (chkVolt.IsChecked == true)
                varSql = varSql + $"VOLT LIKE '%{elementoDeBusca}%' OR ";
            if (chkFrom.IsChecked == true)
                varSql = varSql + $"\"FROM\" LIKE '%{elementoDeBusca}%' OR ";
            if (chkTo.IsChecked == true)
                varSql = varSql + $"\"TO\" LIKE '%{elementoDeBusca}%' OR ";
            if (chkRemarks.IsChecked == true)
                varSql = varSql + $"\"REMARKS\" LIKE '%{elementoDeBusca}%' OR ";

            string resultado = varSql.Substring(0, varSql.Length - 3) + ") ";

            if(!string.IsNullOrEmpty(txtNotLike.Text))

            {
                resultado = resultado + $" and \"{cmbNotLike.Text}\" not like '%{txtNotLike.Text}%'";
            }

            DataTable retorno = acessos.ExecuteQuery(resultado);

            gridMain.ItemsSource = retorno.DefaultView;
        }

        public void findProjects()
        {
            string query = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'cableList_%'";
            DataTable retorno = acessos.ExecuteQuery(query);
            List<string> projects = new();
            foreach (DataRow linha in retorno.Rows)
            {
                string[] splitado = linha[0].ToString().Split("cableList_");
                projects.Add(splitado[1]);
            }

            cmbProjects.ItemsSource = projects;

            cmbProjects.SelectedIndex = -1;


        }
        public int idSelecionado = 0;
        private void gridMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        
                if (gridMain.SelectedItem is DataRowView rowView)
                {
                    if (int.TryParse(rowView["id"].ToString(), out idSelecionado))
                    {
                        // Agora você tem o valor do ID na variável

                        string varSql = $"Select * from cableList_{cmbProjects.Text} where id = '{idSelecionado}'";

                        DataTable retorno = acessos.ExecuteQuery(varSql);


                        DataRow row = retorno.Rows[0];

                        txtIssue.Text = row["ISSUE"].ToString();
                        txtTag.Text = row["TAG"].ToString();
                        txtQty.Text = row["QTY"].ToString();
                        txtWireSize.Text = row["WIRE SIZE"].ToString();
                        txtGndQty.Text = row["GND QTY"].ToString();
                        txtGrdSize.Text = row["GND SIZE"].ToString();
                        txtInsul.Text = row["INSUL."].ToString();
                        txtVoltage.Text = row["VOLT"].ToString();
                        txtFrom.Text = row["FROM"].ToString();
                        txtTo.Text = row["TO"].ToString();
                        txtDwgFrom.Text = row["DWG FROM"].ToString();
                        txtDwgTo.Text = row["DWG TO"].ToString();
                        txtRemarks.Text = row["REMARKS"].ToString();



                    }
                }
          
        }


        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"Delete from cableList_{cmbProjects.Text} where id = '{idSelecionado}'";

            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MySnackbar.MessageQueue?.Enqueue("Deleted Successfully");
            else
            {
                var redMessage = new TextBlock
                {
                    Text = "Error",
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold // opcional
                };

                MySnackbar.MessageQueue?.Enqueue(redMessage);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            string varSql =
                $"UPDATE cableList_modeste SET " +
                $"\"ISSUE\" = '{txtIssue.Text.Replace("'", "''")}', " +
                $"\"TAG\" = '{txtTag.Text.Replace("'", "''")}', " +
                $"\"QTY\" = '{txtQty.Text.Replace("'", "''")}', " +
                $"\"WIRE SIZE\" = '{txtWireSize.Text.Replace("'", "''")}', " +
                $"\"GND QTY\" = '{txtGndQty.Text.Replace("'", "''")}', " +
                $"\"GND SIZE\" = '{txtGrdSize.Text.Replace("'", "''")}', " +
                $"\"INSUL.\" = '{txtInsul.Text.Replace("'", "''")}', " +
                $"\"VOLT\" = '{txtVoltage.Text.Replace("'", "''")}', " +
                $"\"FROM\" = '{txtFrom.Text.Replace("'", "''")}', " +
                $"\"TO\" = '{txtTo.Text.Replace("'", "''")}', " +
                $"\"DWG FROM\" = '{txtDwgFrom.Text.Replace("'", "''")}', " +
                $"\"DWG TO\" = '{txtDwgTo.Text.Replace("'", "''")}', " +
                $"\"REMARKS\" = '{txtRemarks.Text.Replace("'", "''")}' " +
                $"WHERE id = {idSelecionado};";
            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MySnackbar.MessageQueue?.Enqueue("Updated Successfully");
            else
            {
                var redMessage = new TextBlock
                {
                    Text = "Error Updating",
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold // opcional
                };

                MySnackbar.MessageQueue?.Enqueue(redMessage);
            }
        }

        private void txtBusca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Dispara o clique do botão
                Button_Click(sender, e);
            }
        }

        private void txtNotLike_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Dispara o clique do botão
                Button_Click(sender, e);
            }
        }
    }
}
