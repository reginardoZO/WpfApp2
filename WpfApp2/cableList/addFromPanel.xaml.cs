using System;
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

namespace WpfApp2.cableList
{
    /// <summary>
    /// Interaction logic for addFromPanel.xaml
    /// </summary>
    public partial class addFromPanel : UserControl
    {
        DatabaseAccess acessos = new();
        public addFromPanel()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"insert into clFrom (project, plant, type, tag, pnlVoltage, pnlPosition) values('{projeto}', '{planta}', '{tipo}', '{txtTag.Text}', '{cmbVoltage.Text}','{txtPosition.Text}')";

            int resposta = acessos.ExecuteNonQuery(varSql) ;

            if (resposta > 0)
            {
                MessageBox.Show("Success");
            }
            else
            {
                MessageBox.Show("Error");
            }
        }

        public string projeto { get; set; }
        public string planta { get; set; }
        public string tipo { get; set; }
    }
}
