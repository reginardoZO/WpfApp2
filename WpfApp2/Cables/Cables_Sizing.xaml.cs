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
using CableCalculator;
using Newtonsoft.Json;

namespace WpfApp2.Cables
{
    /// <summary>
    /// Interaction logic for Cables_Sizing.xaml
    /// </summary>
    public partial class Cables_Sizing : UserControl
    {
        public Cables_Sizing()
        {
            InitializeComponent();
            double fator = 0.75;
            for (int i = 0; i < 6; i++)
            {
                cmbCorrection.Items.Add(fator.ToString("0.00"));
                fator += 0.05;
            }
            cmbType.Items.Add("Motor");
            cmbType.Items.Add("Heater");
            cmbType.Items.Add("Feeder");
            cmbLevel.Items.Add("380");
            cmbLevel.Items.Add("440");
            cmbLevel.Items.Add("480");

            cmbCable.Items.Add("VFD");
            cmbCable.Items.Add("Power Single");
            cmbCable.Items.Add("Power Multicable");

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var calculator = new ElectricalCableCalculator();
            // Prepare input parameters
            var input = new CableCalculationInput
            { 
                LoadType = cmbType.SelectedItem.ToString(),
                Unit = cmbUnit.SelectedItem.ToString(),
                Voltage = Convert.ToDouble(cmbLevel.SelectedItem.ToString()),
                Power = Convert.ToDouble(txtPower.Text),
                Temperature = (g90.IsChecked == true) ? 90 : 75,
                Factor = Convert.ToDouble(cmbCorrection.SelectedItem.ToString()),
                PowerFactor = Convert.ToDouble(txtPowerFactor.Text),
                CableType = cmbCable.SelectedItem.ToString()
            };
            // Calculate cable
            string resultado = calculator.CalculateCable(input);

            var result = JsonConvert.DeserializeObject<RootObject>(resultado);


            if (result == null || !result.Success || result.Calculation == null)
            {
                string errorMessage = result?.Error ?? "An unknown error occurred during the calculation.";
                MessageBox.Show(errorMessage, "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Interrompe o restante do código
            }
            else
            {
                var doc = new FlowDocument();

                // === CALCULATION ===
                doc.Blocks.Add(new Paragraph(new Run("=== CALCULATION ===")));

                doc.Blocks.Add(new Paragraph(new Run($"Load type: {result.Calculation.LoadType}")));
                doc.Blocks.Add(new Paragraph(new Run($"Power: {result.Calculation.Power} {result.Calculation.Unit}")));
                doc.Blocks.Add(new Paragraph(new Run($"Voltage: {result.Calculation.Voltage} V")));
                doc.Blocks.Add(new Paragraph(new Run($"Power factor: {result.Calculation.PowerFactor}")));

                // Required current (bold + bigger)
                var pRequired = new Paragraph();
                var runRequired = new Run($"Required current: {result.Calculation.RequiredCurrent} A")
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 16
                };
                pRequired.Inlines.Add(runRequired);
                doc.Blocks.Add(pRequired);

                // Adjusted current (bold + bigger)
                var pAdjusted = new Paragraph();
                var runAdjusted = new Run($"Adjusted current: {result.Calculation.AdjustedCurrent} A")
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 16
                };
                pAdjusted.Inlines.Add(runAdjusted);
                doc.Blocks.Add(pAdjusted);

                doc.Blocks.Add(new Paragraph(new Run($"Correction factor: {result.Calculation.Factor}")));
                doc.Blocks.Add(new Paragraph(new Run($"Temperature: {result.Calculation.Temperature} ºC")));
                doc.Blocks.Add(new Paragraph(new Run($"Cable type: {result.Calculation.CableType}")));

                // === SELECTED CABLE ===
                doc.Blocks.Add(new Paragraph(new Run("\n=== SELECTED CABLE ===")));
                doc.Blocks.Add(new Paragraph(new Run($"Stock number: {result.SelectedCable.StockNumber}")));

                // Conductor size (bold + bigger)
                var pConductor = new Paragraph();
                var runConductor = new Run($"Conductor size:{result.SelectedCable.Quantity} x {result.SelectedCable.ConductorSize}")
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 16
                };
                pConductor.Inlines.Add(runConductor);
                doc.Blocks.Add(pConductor);

                doc.Blocks.Add(new Paragraph(new Run($"Ampacity at 75ºC: {result.SelectedCable.Ampacity75C} A")));
                doc.Blocks.Add(new Paragraph(new Run($"Ampacity at 90ºC: {result.SelectedCable.Ampacity90C} A")));
                doc.Blocks.Add(new Paragraph(new Run($"Overall diameter: {result.SelectedCable.OverallDiameter} in")));
                doc.Blocks.Add(new Paragraph(new Run($"Copper weight: {result.SelectedCable.CopperWeight} lb/1000ft")));
                doc.Blocks.Add(new Paragraph(new Run($"Total weight: {result.SelectedCable.TotalWeight} lb/1000ft")));
                doc.Blocks.Add(new Paragraph(new Run($"Cable type: {result.SelectedCable.CableType}")));

                // === SELECTED CABLE ===
                doc.Blocks.Add(new Paragraph(new Run("\n=== GROUNDING ===")));
                doc.Blocks.Add(new Paragraph(new Run($"Conductor Size: {result.GroundingConductor.ConductorSize}")));
                doc.Blocks.Add(new Paragraph(new Run($"Quantity: {result.GroundingConductor.Quantity}")));

                // Exibir no RichTextBox
                richText.Document.Blocks.Clear();


                richText.Document = doc;

            }





        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbType.SelectedItem == "Motor")
            {
                txtLevel.Text = "Power";
                txtPowerFactor.Visibility = Visibility.Visible;
                lblPowerFactor.Visibility = Visibility.Visible;
                cmbUnit.Items.Clear();
                cmbUnit.Items.Add("HP");
                cmbUnit.Items.Add("kW");
                cmbUnit.SelectedIndex = 0;
            }
            else if (cmbType.SelectedItem == "Heater")
            {
                txtPowerFactor.Visibility = Visibility.Collapsed;
                lblPowerFactor.Visibility = Visibility.Collapsed;
                cmbUnit.Items.Clear();
                txtLevel.Text = "Power";
                cmbUnit.Items.Add("kW");
                cmbUnit.SelectedIndex = 0;
            }
            else
            {
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
            public double Power { get; set; }
            public string Unit { get; set; }
            public double Voltage { get; set; }
            public double PowerFactor { get; set; }
            public double RequiredCurrent { get; set; }
            public double AdjustedCurrent { get; set; }
            public double Factor { get; set; }
            public int Temperature { get; set; }
            public string CableType { get; set; }
            public int CableQuantity { get; set; }
            public double CurrentPerCable { get; set; }
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
    }
}
