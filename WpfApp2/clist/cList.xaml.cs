using System;
using System.Collections.Generic;
using System.Configuration;
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

namespace WpfApp2.clist
{
    /// <summary>
    /// Interaction logic for cList.xaml
    /// </summary>
    public partial class cList : UserControl
    {
        public string project = "";
        public string plant = "";
        DatabaseAccess acessos = new();
        public cList()
        {
            InitializeComponent();
        }

        private void locations_GotFocus(object sender, RoutedEventArgs e)
        {

            cmbLocationProject.ItemsSource = acessos.ExecuteQueryStr("select name from projects", "Name");




        }

        private void cmbLocationProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbLocationPlant.ItemsSource = null;


            string codigoPlanta = acessos.ExecuteScalarStr($"select code from projects where name = '{cmbLocationProject.SelectedValue}'");

            cmbLocationPlant.ItemsSource = acessos.ExecuteQueryStr($"select plant from plants where code = '{codigoPlanta}'", "Plant");

        }

        private void cmbLocationPlant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gridLocations.ItemsSource = null;
            gridLocations.ItemsSource = acessos.ExecuteQueryStr($"select distinct location from clLocation where project ='{cmbLocationProject.Text}' and plant = '{cmbLocationPlant.Text}'", "location");
        }

        private void btnSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"Insert into clLocation (project, plant, location) values ('{cmbLocationProject.Text}', '{cmbLocationPlant.Text}', '{txtNewLocation.Text}')";
            int retorno = acessos.ExecuteNonQuery(varSql);
            if (retorno > 0)
                MessageBox.Show("Success");
            else
                MessageBox.Show("Error");

            gridLocations.ItemsSource = null;
            gridLocations.ItemsSource = acessos.ExecuteQueryStr($"select distinct location from clLocation where project ='{cmbLocationProject.Text}' and plant = '{cmbLocationPlant.Text}'", "location");


        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"select distinct location from clLocation where project ='{cmbLocationProject.Text}' and plant = '{cmbLocationPlant.Text}'";

            DataTable dt = acessos.ExecuteQuery(varSql);

            gridLocations.ItemsSource = dt.AsDataView();
        }
        //got focus on FROM
        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            List<string> checagem = acessos.ExecuteQueryStr($"select distinct location from clLocation where project = '{project}' and plant = '{plant}'", "location");
            cmbLocationFromPanel.ItemsSource = checagem;
            cmbLocationFromTransformer.ItemsSource = checagem;
            cmbLocationFromUtility.ItemsSource = checagem;
            cmbLocationFromGeneral.ItemsSource = checagem;

            cmbTypeFromGeneral.ItemsSource = acessos.ExecuteQueryStr("select Equips from clEquips", "Equips");

        }

        private void Grid_GotFocus_1(object sender, RoutedEventArgs e)
        {
            cmbProjectCircuit.ItemsSource = acessos.ExecuteQueryStr("select distinct name from projects", "Name");


        }

        private void cmbProjectCircuit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string codigoProjeto = acessos.ExecuteScalarStr($"Select Code from projects where name = '{cmbProjectCircuit.SelectedValue}'");

            cmbPlantCircuit.ItemsSource = acessos.ExecuteQueryStr($"select distinct plant from plants where code = '{codigoProjeto}'", "Plant");
        }

        private void cmbPlantCircuit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            project = cmbProjectCircuit.Text;
            plant = cmbPlantCircuit.SelectedValue.ToString();

            btnCircuitsReload_Click(sender, e);
        }

        private void btnSaveFromPanel_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"insert into clFrom (project, plant, type, tag, voltage, location, description) values ('{project}', '{plant}', 'Panel', '{txtTagFromPanel.Text}', '{cmbVoltageLevelFromPanel.Text}', '{cmbLocationFromPanel.Text}', '{txtDescriptionFromPanel.Text}')";
            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MessageBox.Show("Success");
            else
                MessageBox.Show("Error");

        }

        private void btnFromTransformerSave_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"insert into  clFrom (project, plant, type, tag, trpower, trpowerunit, location, trHighVoltage, trLowVoltage, description, voltage) values ('{project}', '{plant}', 'Transformer', '{txtTagFromTransformer.Text}', '{txtPowerFromTransformer.Text}', '{cmbPowerUnitsFromTransformer.Text}', '{cmbLocationFromTransformer.Text}', '{txtPrimaryFromTransformer.Text}', '{txtSecondaryFromTransformer.Text}', '{txtDescriptionFromTransformer.Text}', '{txtSecondaryFromTransformer.Text}')";
            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MessageBox.Show("Success");
            else
                MessageBox.Show("Error");

        }

        private void btnFromSaveUtility_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"insert into clfrom (project, plant, type, tag, voltage, location, description) values ('{project}','{plant}', 'Utility - {cmbTypeFromUtility.Text}', '{txtTagFromUtility.Text}', '{cmbVoltageLevelFromUtility.Text}', '{cmbLocationFromUtility.Text}', '{txtDescriptionFromUtility.Text}')";
            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MessageBox.Show("Success");
            else
                MessageBox.Show("Error");
        }

        private void btnSaveFromGeneral_Click_1(object sender, RoutedEventArgs e)
        {
            string varSql = $"insert into clfrom (project, plant, type, tag, voltage, location, description) values ('{project}','{plant}', '{cmbTypeFromGeneral.Text}', '{txtTagFromGeneral.Text}', '{cmbVoltageLevelFromGeneral.Text}', '{cmbLocationFromGeneral.Text}', '{txtDescriptionFromGeneral.Text}')";
            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MessageBox.Show("Success");
            else
                MessageBox.Show("Error");
        }

        //focus on TO tab
        private void Grid_GotFocus_2(object sender, RoutedEventArgs e)
        {
            List<string> checagem = acessos.ExecuteQueryStr($"select distinct location from clLocation where project = '{project}' and plant = '{plant}'", "location");
            cmbLocationToGeneral.ItemsSource = checagem;

            cmbTypeToGeneral.ItemsSource = acessos.ExecuteQueryStr("select Equips from clEquips", "Equips");
        }

        private void btnSaveToGeneral_Click(object sender, RoutedEventArgs e)
        {
            string varSql = $"insert into clTo (project, plant, type, tag, voltage, location, description) values ('{project}','{plant}', '{cmbTypeToGeneral.Text}', '{txtTagToGeneral.Text}', '{cmbVoltageLevelToGeneral.Text}', '{cmbLocationToGeneral.Text}', '{txtDescriptionToGeneral.Text}')";
            int retorno = acessos.ExecuteNonQuery(varSql);

            if (retorno > 0)
                MessageBox.Show("Success");
            else
                MessageBox.Show("Error");
        }

        private void btnCircuitsReload_Click(object sender, RoutedEventArgs e)
        {

            cmbTypeFromCircuits.ItemsSource = acessos.ExecuteQueryStr($"select distinct type from clFrom where project = '{cmbProjectCircuit.Text}' and plant = '{cmbPlantCircuit.Text}'", "type");
            cmbTypeToCircuits.ItemsSource = acessos.ExecuteQueryStr($"select distinct type from clto where project = '{cmbProjectCircuit.Text}' and plant = '{cmbPlantCircuit.Text}'", "type");


            cmbTypeFromCircuits.SelectedIndex = -1;
            cmbTagFromCircuits.SelectedIndex = -1;
            cmbTypeToCircuits.SelectedIndex = -1;
            cmbTagToCircuits.SelectedIndex = -1;

            cmbQtySingleCircuits.SelectedIndex = -1;
            cmbSingleSizeCircuits.SelectedIndex = -1;
            cmbGndQtSingleCircuits.SelectedIndex = -1;
            cmbSingleSizeCircuitsGnd.SelectedIndex = -1;

            cmbQtyMultiCircuits.SelectedIndex = -1;
            cmbConductorsMultiCircuits.SelectedIndex = -1;
            cmbMultiSizeCircuits.SelectedIndex = -1;
            cmbGndQtMultiCircuits.SelectedIndex = -1;
            cmbMultiSizeCircuitsGnd.SelectedIndex = -1;

        }

        private void cmbTypeFromCircuits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            cmbTagFromCircuits.ItemsSource = acessos.ExecuteQueryStr($"select distinct tag from clFrom where project = '{cmbProjectCircuit.Text}' and plant = '{cmbPlantCircuit.Text}' and type = '{cmbTypeFromCircuits.SelectedValue}'", "tag");

        }

        private void cmbTypeToCircuits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbTagToCircuits.ItemsSource = acessos.ExecuteQueryStr($"select distinct tag from clto where project = '{cmbProjectCircuit.Text}' and plant = '{cmbPlantCircuit.Text}' and type = '{cmbTypeToCircuits.SelectedValue}'", "tag");
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void singleRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (singleRadio.IsChecked == true)
            {
                canvaSingle.Visibility = Visibility.Visible;
                canvaMulti.Visibility = Visibility.Hidden;
            }
            else
            {
                canvaSingle.Visibility = Visibility.Hidden;
                canvaMulti.Visibility = Visibility.Visible;

            }
        }

        private void multiRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (singleRadio.IsChecked == true)
            {
                canvaSingle.Visibility = Visibility.Visible;
                canvaMulti.Visibility = Visibility.Hidden;
            }
            else
            {
                canvaSingle.Visibility = Visibility.Hidden;
                canvaMulti.Visibility = Visibility.Visible;

            }
        }

        private void btnCircuitsCreateTag_Click(object sender, RoutedEventArgs e)
        {

            cableTagParts cableTag = new();

            // get voltage level on Source
            string fromLevel = acessos.ExecuteScalarStr($"select voltage from clFrom where project = '{cmbProjectCircuit.Text}' and plant = '{cmbPlantCircuit.Text}' and type = '{cmbTypeFromCircuits.Text}' and tag = '{cmbTagFromCircuits.Text}'");

            string[] splittedLevel = fromLevel.Split('k');

            double voltageLevel = 0;


            if (splittedLevel.Length == 2)
                voltageLevel = Convert.ToDouble(splittedLevel[0]) * 1000;
            else
            {
                int index = splittedLevel[0].IndexOf('V');

                string result = index >= 0 ? splittedLevel[0].Substring(0, index) : splittedLevel[0];

                voltageLevel = Convert.ToDouble(result);

            }

            if (voltageLevel >= 35000)
            {
                cableTag.voltageGrade = "0";
            } // 0
            else if (voltageLevel >= 15000 && voltageLevel < 35000)
            {
                cableTag.voltageGrade = "1";
            } //1
            else if (voltageLevel >= 5000 && voltageLevel < 15000)
            {
                if (cmbTypeFromCircuits.Text == "VFD")
                {
                    if (voltageLevel >= 8000 && voltageLevel <= 15000)
                        cableTag.voltageGrade = "2A";
                    else if (voltageLevel >= 5000 && voltageLevel < 8000)
                        cableTag.voltageGrade = "2B";
                }
                else
                {
                    cableTag.voltageGrade = "2";
                }
            } //2, 2A, 2B
            else if (voltageLevel > 600 && voltageLevel <= 5000)
            {
                if (cmbTypeFromCircuits.Text == "VFD")
                    cableTag.voltageGrade = "3A";
                else
                    cableTag.voltageGrade = "3";
            } // 3, 3A
            else if(voltageLevel <= 600)
            {
                if(singleRadio.IsChecked == true)
                {
                    cableTag.voltageGrade = "4";

                }
                else
                {

                    if (cmbTypeFromCircuits.Text == "VFD")
                    {
                        cableTag.voltageGrade = "7";
                    }
                    else
                        cableTag.voltageGrade = "5";
                }

            } // 4, 5, 7
            else if(voltageLevel <= 300)
            {
                cableTag.voltageGrade = "8";
            } // 8


            lblCircuitsTag.Content = cableTag.voltageGrade;


        }

        public class cableTagParts
        {
            public string voltageGrade { get; set; }
            public string equipMentTag { get; set; }
            public string cableApplication { get; set; }

            public string countingNumber { get; set; }
        }


    }
}
