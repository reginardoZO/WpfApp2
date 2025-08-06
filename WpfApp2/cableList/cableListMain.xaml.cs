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
    /// Interaction logic for cableListMain.xaml
    /// </summary>
    public partial class cableListMain : UserControl
    {
        string varSql = "";
        DatabaseAccess acessos = new();
        public cableListMain()
        {
            
            InitializeComponent();

            varSql = "select distinct name from projects";

            cmbProject.ItemsSource = acessos.ExecuteQueryStr(varSql, "Name");
        }

        private void cmbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbPlant.ItemsSource = null;

            varSql = $"select code from projects where name = '{cmbProject.SelectedValue}'";

            string codigo = acessos.ExecuteScalarStr(varSql);

            varSql = $"select distinct plant from plants where code = '{codigo}'";

            cmbPlant.ItemsSource = acessos.ExecuteQueryStr(varSql , "Plant");
           
        }

        private void cmbFromType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            varSql = $"select distinct tag from clFrom where project = '{cmbProject.Text}' and plant = '{cmbPlant.Text}' and type = '{cmbFromType.Text}'";

            cmbFromEquip.ItemsSource = acessos.ExecuteQueryStr(varSql, "tag");
        }

        private void btnNewEquipFrom_Click(object sender, RoutedEventArgs e)
        {
            string sendoToPopup = cmbProject.Text + "|" + cmbPlant.Text + "|"+cmbFromType.Text;
            RegisterFrom registerPop = new RegisterFrom(sendoToPopup);
            registerPop.Owner = Window.GetWindow(this);
            registerPop.ShowDialog();
        }

        private void btnNewLocation_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
