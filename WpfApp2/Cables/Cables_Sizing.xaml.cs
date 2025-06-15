using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
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
using CableCalculator;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using CablesQuery;
using System.Reflection.Metadata.Ecma335;
using ControlzEx.Standard;
using System.Security.RightsManagement;
using System.Text.RegularExpressions;


namespace WpfApp2.Cables
{




    public partial class Cables_Sizing : UserControl
    {

        CablesQueryEngine jsonQuery = new CablesQueryEngine();


        DatabaseAccess acessos = new DatabaseAccess();

        public Cables_Sizing()
        {
            InitializeComponent();

            cmbType.Items.Add("Motor");
            cmbType.Items.Add("Heater");
            cmbType.Items.Add("Feeder");
            cmbLevel.Items.Add("380");
            cmbLevel.Items.Add("440");
            cmbLevel.Items.Add("480");

            cmbCable.Items.Add("VFD");
            cmbCable.Items.Add("Single");
            cmbCable.Items.Add("Multicable");

            updateProjects();

            MySnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));


        }

        public async void updateProjects()
        {
            string varSql = "Select * from projects";

            DataTable retorno = await Task.Run(() =>
            {
                return acessos.ExecuteQuery(varSql);
            });


            List<string> projectsList = retorno.AsEnumerable()
                                    .Select(row => row.Field<string>("Name"))
                                    .ToList();
            cmbProjects.ItemsSource = projectsList;

            cmbProjects.SelectedIndex = -1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {


            calcCables calcCabos = new calcCables();

            inputData dadosEntrada = new();
            dadosEntrada.voltageLevel = Convert.ToDouble(cmbLevel.SelectedItem.ToString());
            dadosEntrada.cableType = cmbCable.SelectedItem.ToString();
            dadosEntrada.loadType = cmbType.SelectedItem.ToString();
            dadosEntrada.powerFactor = Convert.ToDouble(txtPowerFactor.Text);
            dadosEntrada.power = Convert.ToDouble(txtPower.Text);
            dadosEntrada.efficiency = string.IsNullOrWhiteSpace(txtEff.Text) ? 0 : Convert.ToDouble(txtEff.Text);
            dadosEntrada.powerUnit = cmbUnit.SelectedItem.ToString();
            if (g90.IsChecked == true)
                dadosEntrada.cableTempCol = 90;
            else
                dadosEntrada.cableTempCol = 75;

            dadosEntrada.ambientTemperature = string.IsNullOrWhiteSpace(txtAmbTemp.Text) ? 85 : Convert.ToDouble(txtAmbTemp.Text);
            dadosEntrada.maxDropVoltage = string.IsNullOrWhiteSpace(txtMaxDrop.Text) ? 3 : Convert.ToDouble(txtMaxDrop.Text);
            dadosEntrada.distance = string.IsNullOrWhiteSpace(txtDistanceSizer.Text) ? 1 : Convert.ToDouble(txtDistanceSizer.Text);



            outputData dadosSaida = new();

            dadosSaida.nominalCurrent = calcCabos.nominalCurrent(dadosEntrada);
            dadosSaida.temperatureFactor = calcCabos.temperatureFactor(dadosEntrada);
            dadosSaida.correctedCurrent = dadosSaida.nominalCurrent / dadosSaida.temperatureFactor;
            dadosSaida.sizedCableCurrent = calcCabos.findCableByCorrectecCurrent(dadosEntrada, dadosSaida);

            lblSizedCable.Text = dadosSaida.sizedCableCurrent;

            var match = Regex.Match(dadosSaida.sizedCableCurrent, @"^(\d+)\s*x\s*\(\s*(.+?)\s*\+\s*(.+?)\s*\)$");
            dadosSaida.cableQuantity = int.Parse(match.Groups[1].Value);
            dadosSaida.powerCable = match.Groups[2].Value.Trim();
            dadosSaida.groundCable = match.Groups[3].Value.Trim();

            //auxiliar

            dadosSaida.sizedDrop = calcCabos.findCableByDropVoltage(dadosEntrada, dadosSaida);


            var doc = new FlowDocument();

            // === CALCULATION ===
            doc.Blocks.Add(new Paragraph(new Run("=== CALCULATION ===")));

            doc.Blocks.Add(new Paragraph(new Run($"Load type: {dadosEntrada.loadType}")));
            doc.Blocks.Add(new Paragraph(new Run($"Power: {dadosEntrada.power:F2} {dadosEntrada.powerUnit}")));
            doc.Blocks.Add(new Paragraph(new Run($"Power Factor: {dadosEntrada.powerFactor:F2}")));
            doc.Blocks.Add(new Paragraph(new Run($"Voltage Level: {dadosEntrada.voltageLevel} V")));
            doc.Blocks.Add(new Paragraph(new Run($"Efficiency: {dadosEntrada.efficiency} %")));

            var pRequired = new Paragraph();
            var runRequired = new Run($"Nominal current: {dadosSaida.nominalCurrent:F2} A")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };
            pRequired.Inlines.Add(runRequired);
            doc.Blocks.Add(pRequired);


            pRequired = new Paragraph();
            runRequired = new Run($"Temperature Factor: {dadosSaida.temperatureFactor:F2}")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };
            pRequired.Inlines.Add(runRequired);
            doc.Blocks.Add(pRequired);

            pRequired = new Paragraph();
            runRequired = new Run($"New Current: {dadosSaida.correctedCurrent:F2}")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };
            pRequired.Inlines.Add(runRequired);
            doc.Blocks.Add(pRequired);


            pRequired = new Paragraph();
            runRequired = new Run($"Cable by Ampacity: {dadosSaida.sizedCableCurrent}")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };
            pRequired.Inlines.Add(runRequired);
            doc.Blocks.Add(pRequired);


            Brush corSelecionada;

            if (dadosSaida.sizedDrop > dadosEntrada.maxDropVoltage)
            {
                corSelecionada = Brushes.Red;
            }
            else
            {
                corSelecionada = Brushes.Black;
            }

            pRequired = new Paragraph();


            runRequired = new Run($"Sized Drop: {dadosSaida.sizedDrop:F2}")
            {

                Foreground = corSelecionada, // Highlight the voltage drop result


                FontWeight = FontWeights.Bold,
                FontSize = 16
            };


            pRequired.Inlines.Add(runRequired);
            doc.Blocks.Add(pRequired);



            richText.Document.Blocks.Clear();


            richText.Document = doc;


        }

        public class inputData
        {
            public string loadType { get; set; }
            public string cableType { get; set; }
            public double powerFactor { get; set; }
            public double power { get; set; }
            public string powerUnit { get; set; }
            public double voltageLevel { get; set; }
            public double efficiency { get; set; }
            public int cableTempCol { get; set; }
            public double ambientTemperature { get; set; }
            public double maxDropVoltage { get; set; }
            public double distance { get; set; } // This can be used to store the distance if applicable
        }

        public class outputData
        {
            public double nominalCurrent { get; set; }
            public double temperatureFactor { get; set; }

            public double correctedCurrent { get; set; }

            public string sizedCableCurrent { get; set; }

            public int cableQuantity { get; set; } // This can be used to store the number of cables needed if applicable
            public string powerCable { get; set; }

            public string groundCable { get; set; }
            public double sizedDrop { get; set; }
        }





        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbType.SelectedItem == "Motor")
            {
                txtLevel.Text = "Power";
                txtPowerFactor.Visibility = Visibility.Visible;
                lblPowerFactor.Visibility = Visibility.Visible;
                txtEff.Visibility = Visibility.Visible;
                lblEff.Visibility = Visibility.Visible;
                lblUnitEfficiency.Visibility = Visibility.Visible;
                cmbUnit.Items.Clear();
                cmbUnit.Items.Add("HP");
                cmbUnit.Items.Add("kW");
                cmbUnit.SelectedIndex = 0;
            }
            else if (cmbType.SelectedItem == "Heater")
            {
                txtEff.Visibility = Visibility.Collapsed;
                lblEff.Visibility = Visibility.Collapsed;
                lblUnitEfficiency.Visibility = Visibility.Collapsed;
                txtPowerFactor.Visibility = Visibility.Collapsed;
                lblPowerFactor.Visibility = Visibility.Collapsed;
                cmbUnit.Items.Clear();
                txtLevel.Text = "Power";
                cmbUnit.Items.Add("kW");
                cmbUnit.SelectedIndex = 0;
            }
            else
            {
                txtEff.Visibility = Visibility.Collapsed;
                lblEff.Visibility = Visibility.Collapsed;
                lblUnitEfficiency.Visibility = Visibility.Collapsed;
                txtPowerFactor.Visibility = Visibility.Collapsed;
                lblPowerFactor.Visibility = Visibility.Collapsed;
                cmbUnit.Items.Clear();
                txtLevel.Text = "Current";
                cmbUnit.Items.Add("A");
                cmbUnit.SelectedIndex = 0;
            }
        }




        public class Calculation
        {
            public string LoadType { get; set; }
            public double RequiredCurrent { get; set; }
            public double AdjustedCurrent { get; set; }

            public string AmbientTemperature { get; set; }

            public double TemperatureCorrectionFactor { get; set; }

            public double OriginalCableAmpacity { get; set; }

            public double CorrectedCableAmpacity { get; set; }
            public int CableQuantity { get; set; }
            public double CurrentPerCable { get; set; }

            public double Distance { get; set; }
            public double VoltageDropCalculated { get; set; }
            public double VoltageDropPercentage { get; set; }
            public double VoltageDropLimit { get; set; }

            public string VoltageDropStatus { get; set; } // "OK" or "Exceeds Limit"

            public double EffectiveImpedance { get; set; }

        }

        public class SelectedCable
        {
            public string StockNumber { get; set; }
            public string ConductorSize { get; set; }
            public double Ampacity75C { get; set; }
            public double Ampacity90C { get; set; }
            public double OverallDiameter { get; set; }
            public double CopperWeight { get; set; }
            public double TotalWeight { get; set; }
            public string CableType { get; set; }
            public int Quantity { get; set; }

            public double CorrectedAmpacity { get; set; }
        }

        public class GroundingConductor
        {
            public string ConductorSize { get; set; }
            public double MaxProtectionDevice { get; set; }
            public string Material { get; set; }
            public string Standard { get; set; }
            public int Quantity { get; set; }
        }




        public class RootObject
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public Calculation Calculation { get; set; }
            public SelectedCable SelectedCable { get; set; }

            public GroundingConductor GroundingConductor { get; set; }
        }

        private void cmbProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updatePlants(cmbProjects.SelectedValue.ToString());
        }

        public async void updatePlants(string selectedProject)
        {
            string varSql = $"Select code from projects where Name = '{selectedProject}'";

            DataTable retorno = await Task.Run(() =>
            {
                return acessos.ExecuteQuery(varSql);
            });

            varSql = $"select plant from plants where code = '{retorno.Rows[0]["code"]}'";

            retorno = await Task.Run(() =>
            {
                return acessos.ExecuteQuery(varSql);
            });


            List<string> plantList = retorno.AsEnumerable()
                                    .Select(row => row.Field<string>("Plant"))
                                    .ToList();

            cmbPlant.ItemsSource = plantList;

            cmbPlant.SelectedIndex = -1;

        }

        private void radLV_Checked(object sender, RoutedEventArgs e)
        {
            if (radLV.IsChecked == true)
            {
                populateGrid(cmbProjects.SelectedValue.ToString(), cmbPlant.SelectedValue.ToString(), "LV");

            }
            else
            {
                populateGrid(cmbProjects.SelectedValue.ToString(), cmbPlant.SelectedValue.ToString(), "MV");
            }
        }

        public async void populateGrid(string project, string plant, string voltage)
        {
            //find project code
            string varSql = $"Select code from projects where Name = '{project}'";

            DataTable oneRowReturn = await Task.Run(() =>
            {
                return acessos.ExecuteQuery(varSql);
            });

            project = oneRowReturn.Rows[0]["code"].ToString();


            varSql = "";
            if (voltage == "LV")
            {
                varSql = $"Select id, frompanel, fromUnit, Loadtype, Power, powerUnit, tag, descr from lvLoads where project = '{project}' and plant = '{plant}'";
            }
            else
            {
                varSql = $"Select id, frompanel, fromUnit, Loadtype, Power, powerUnit, tag, descr from mvLoads where project = '{project}' and plant = '{plant}'";
            }

            DataTable retorno = await Task.Run(() =>
            {
                return acessos.ExecuteQuery(varSql);
            });

            gridCircuits.ItemsSource = retorno.DefaultView;
        }

        private void radMV_Checked(object sender, RoutedEventArgs e)
        {
            if (radLV.IsChecked == true)
            {
                populateGrid(cmbProjects.SelectedValue.ToString(), cmbPlant.SelectedValue.ToString(), "LV");

            }
            else
            {
                populateGrid(cmbProjects.SelectedValue.ToString(), cmbPlant.SelectedValue.ToString(), "MV");
            }
        }

        private void cmbPlant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (radLV.IsChecked == true)
            {
                populateGrid(cmbProjects.SelectedValue.ToString(), cmbPlant.SelectedValue.ToString(), "LV");

            }
            else
            {
                populateGrid(cmbProjects.SelectedValue.ToString(), cmbPlant.SelectedValue.ToString(), "MV");
            }
        }
        int selectedId = 0;
        private void gridCircuits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            if (gridCircuits.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)gridCircuits.SelectedItem;
                selectedId = Convert.ToInt32(selectedRow["id"]);

                cmbType.Text = selectedRow["loadType"].ToString();
                txtPower.Text = selectedRow["power"].ToString();
                cmbUnit.Text = selectedRow["powerUnit"].ToString();


            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string loadLevel = "MV";
            if (radLV.IsChecked == true)
            {
                loadLevel = "LV";
            }
            string varSql = $"Insert into circuits (loadLevel, loadId, tag, cable, underSection, aboveSection, distance) values ('{loadLevel}', {selectedId}, '{txtTagCircuit.Text.ToString()}', '{txtSelectedCable.Text.ToString()}', '{txtUnderSection.Text.ToString()}', '{txtAboveSection.Text.ToString()}', '{txtDistance.Text.ToString()}')";

            int resposta = acessos.ExecuteNonQuery(varSql);

            if (resposta > 0)
            {
                MySnackbar.MessageQueue?.Enqueue("Added Successfully");


            }
            else
            {
                MessageBox.Show("Error");
            }
        }
    }
}

