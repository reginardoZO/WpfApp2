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
using System.Data;
using System.Configuration;

namespace WpfApp2.CableNEC
{
    /// <summary>
    /// Interaction logic for cablesNec.xaml
    /// </summary>
    public partial class cablesNec : UserControl
    {

        DatabaseAccess acessos = new();


        public class parametrosCalculados
        {
            public double Inom { get; set; }
            public double MultiplierFactor { get; set; }
            public double IA2 { get; set; }

            public double voltageLevel { get; set; }

            public double powerFactor { get; set; }

            public double tempAmbiente { get; set; }

            public double deRatingTempFactor { get; set; }

            public double IA3 { get; set; }

            public string cableAbove { get; set; }

            public string cableUnder { get; set; }

            public double InomMV { get; set; }

            public double MultiplierFactorMV { get; set; }

            public double IA2MV { get; set; }

            public double voltageLevelMV { get; set; }
            public double powerFactorMV { get; set; }
            public double tempAmbienteMV { get; set; }
            public double deRatingTempFactorMV { get; set; }
            public double IA3MV { get; set; }
            public string cableAboveMV { get; set; }
            public string cableUnderMV { get; set; }




        }
        public cablesNec()
        {


            InitializeComponent();
            List<string> unidades = new();
            unidades.Add("HP");
            unidades.Add("kW");
            unidades.Add("kVA");
            unidades.Add("A");
            cmbLoadUnit.ItemsSource = unidades;


            List<string> unidadesMV = unidades;
            unidadesMV.Remove("A");
            cmbUnitMV.ItemsSource = unidadesMV;


        }
        parametrosCalculados paparam = new parametrosCalculados();

        private void cmbLoadUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbLoadUnit.SelectedValue == "HP")
            {
                cmbPowerCombo.IsEnabled = true;
                lblPowerCombo.IsEnabled = true;

                cmbLoadType.Items.Clear();
                cmbLoadType.Items.Add("MOTORS");

                txtPower.IsEnabled = false;
                lblTxtPower.IsEnabled = false;

                txtPowerFactor.IsEnabled = false;
                lblTxtPower.IsEnabled = false;


                DataTable potencias = acessos.ExecuteQuery("SELECT DISTINCT Horsepower FROM NEC_430_250");

                List<string> lista = potencias.AsEnumerable()
                              .Select(row => row["Horsepower"].ToString())
                              .ToList();

                cmbPowerCombo.ItemsSource = lista;


            }
            else
            {

                cmbLoadType.Items.Clear();
                cmbLoadType.Items.Add("XMRFS"); cmbLoadType.Items.Add("FEEDER"); cmbLoadType.Items.Add("GENERATOR");

                cmbPowerCombo.IsEnabled = false;
                lblPowerCombo.IsEnabled = false;

                txtPower.IsEnabled = true;
                lblTxtPower.IsEnabled = true;


                txtPowerFactor.IsEnabled = true;
                lblTxtPower.IsEnabled = true;

            }
        }

        private void btnStep1_Click(object sender, RoutedEventArgs e)
        {
            paparam.voltageLevel = Convert.ToDouble(txtVoltageLevel.Text);
            paparam.powerFactor = Convert.ToDouble(txtPowerFactor.Text);

            if (cmbLoadUnit.SelectedValue == "HP")
            {
                string varSql = $"SELECT * FROM NEC_430_250 WHERE Horsepower = '{cmbPowerCombo.SelectedValue}'";

                DataTable resposta = acessos.ExecuteQuery(varSql);

                if (resposta.Rows.Count > 0)
                {
                    paparam.Inom = Convert.ToDouble(resposta.Rows[0]["460V"]);
                    paparam.MultiplierFactor = 1.25; // Assuming a multiplier factor of 1.25 for motors
                    paparam.IA2 = paparam.Inom * paparam.MultiplierFactor;

                    lblINom.Content = "Inom: " + paparam.Inom.ToString("F2") + " A";
                    lblMultiplierFactor.Content = "Multiplier Factor: " + paparam.MultiplierFactor.ToString("F2");
                    lblNewCurrent.Content = "New Current (IA2): " + paparam.IA2.ToString("F2") + " A";
                }
                else
                {
                    MessageBox.Show("No data found for the selected horsepower.");
                }

            }
            else if (cmbLoadUnit.SelectedValue == "A")
            {
                paparam.Inom = Convert.ToDouble(txtPower.Text);
                paparam.MultiplierFactor = 1.25; // Assuming a multiplier factor of 1.25 for general loads
                paparam.IA2 = paparam.Inom * paparam.MultiplierFactor;
                lblINom.Content = "Inom: " + paparam.Inom.ToString("F2") + " A";
                lblMultiplierFactor.Content = "Multiplier Factor: " + paparam.MultiplierFactor.ToString("F2");
                lblNewCurrent.Content = "New Current (IA2): " + paparam.IA2.ToString("F2") + " A";
            }
            else if (cmbLoadUnit.SelectedValue == "kVA")
            {
                paparam.Inom = Convert.ToDouble(txtPower.Text) * 1000 / (paparam.voltageLevel * Math.Sqrt(3)); // Assuming a three-phase system
                paparam.MultiplierFactor = 1.25; // Assuming a multiplier factor of 1.25 for general loads
                paparam.IA2 = paparam.Inom * paparam.MultiplierFactor;
                lblINom.Content = "Inom: " + paparam.Inom.ToString("F2") + " A";
                lblMultiplierFactor.Content = "Multiplier Factor: " + paparam.MultiplierFactor.ToString("F2");
                lblNewCurrent.Content = "New Current (IA2): " + paparam.IA2.ToString("F2") + " A";


            }
            else if (cmbLoadUnit.SelectedValue == "kW")
            {

                paparam.Inom = Convert.ToDouble(txtPower.Text) * 1000 / (paparam.voltageLevel * Math.Sqrt(3) * paparam.powerFactor); // Assuming a three-phase system
                if (cmbLoadType.SelectedValue == "GENERATOR")
                    paparam.MultiplierFactor = 1.15; // Assuming a multiplier factor of 1.25 for general loads
                else
                    paparam.MultiplierFactor = 1.25; // Assuming a multiplier factor of 1.25 for general loads
                paparam.IA2 = paparam.Inom * paparam.MultiplierFactor;
                lblINom.Content = "Inom: " + paparam.Inom.ToString("F2") + " A";
                lblMultiplierFactor.Content = "Multiplier Factor: " + paparam.MultiplierFactor.ToString("F2");
                lblNewCurrent.Content = "New Current (IA2): " + paparam.IA2.ToString("F2") + " A";
            }
            else
            {
                MessageBox.Show("Please select a valid load unit.");
                return;
            }

            if (paparam.IA2 >= 100)
            {
                radio60.IsEnabled = false;
                radio75.IsChecked = true;
            }
            else
            {
                radio60.IsEnabled = true;
                radio75.IsEnabled = true;
            }


        }

        private void btnStep2_Click(object sender, RoutedEventArgs e)
        {
            paparam.tempAmbiente = Convert.ToDouble(txtAmbientTemp.Text);

            paparam.deRatingTempFactor = Math.Sqrt((90 - paparam.tempAmbiente) / 60);

            paparam.IA3 = paparam.IA2 / paparam.deRatingTempFactor;

            lblDeratingTemp.Content = "De-Rating Temp Factor: " + paparam.deRatingTempFactor.ToString("F2");

            lblNewCurrentAfterDeratingTemp.Content = "New Current (IA3): " + paparam.IA3.ToString("F2") + " A";
        }

        private void btnAboveSizing_Click(object sender, RoutedEventArgs e)
        {


            DataTable nec_310_17 = acessos.ExecuteQuery("SELECT * FROM NEC_310_17");

            double currentUsed = paparam.IA3 / Convert.ToDouble(txtAboveDerating.Text);

            if (radio60.IsChecked == true)
            {

                int cablesPerPhase = 1;

                double currentPerCable = currentUsed;

                while (true)
                {
                    foreach (DataRow linha in nec_310_17.Rows)
                    {
                        double ampacity;
                        if (!double.TryParse(linha["60"].ToString(), out ampacity))
                            continue;

                        if (ampacity >= currentPerCable)
                        {
                            paparam.cableAbove = $"{cablesPerPhase} - {linha["size"]}";
                            lblCableAbove.Content = "Cable Above: " + paparam.cableAbove;
                            return; // encontrou, sai do método
                        }
                    }

                    // Se não encontrou nenhum cabo que suporte a corrente atual por cabo,
                    // aumenta o número de cabos por fase e tenta novamente
                    cablesPerPhase++;
                    currentPerCable = currentUsed / cablesPerPhase;

                    // Limite de segurança para evitar loop infinito
                    if (cablesPerPhase > 20)
                    {
                        throw new Exception("Não foi possível encontrar um cabo adequado mesmo com até 20 cabos por fase.");
                    }
                }

            }
            else
            {
                int cablesPerPhase = 1;

                double currentPerCable = currentUsed;

                while (true)
                {
                    foreach (DataRow linha in nec_310_17.Rows)
                    {
                        double ampacity;
                        if (!double.TryParse(linha["75"].ToString(), out ampacity))
                            continue;

                        if (ampacity >= currentPerCable)
                        {
                            paparam.cableAbove = $"{cablesPerPhase} - {linha["size"]}";
                            lblCableAbove.Content = "Cable Above: " + paparam.cableAbove;
                            return; // encontrou, sai do método
                        }
                    }

                    // Se não encontrou nenhum cabo que suporte a corrente atual por cabo,
                    // aumenta o número de cabos por fase e tenta novamente
                    cablesPerPhase++;
                    currentPerCable = currentUsed / cablesPerPhase;

                    // Limite de segurança para evitar loop infinito
                    if (cablesPerPhase > 20)
                    {
                        throw new Exception("Não foi possível encontrar um cabo adequado mesmo com até 10 cabos por fase.");
                    }
                }

            }




        }

        private void btnUnderStep3_Click(object sender, RoutedEventArgs e)
        {


            DataTable nec_310_16 = acessos.ExecuteQuery("SELECT * FROM NEC_310_16");

            double currentUsed = paparam.IA3 / Convert.ToDouble(txtUnderDerating.Text);

            if (radio60.IsChecked == true)
            {

                int cablesPerPhase = 1;

                double currentPerCable = currentUsed;

                while (true)
                {
                    foreach (DataRow linha in nec_310_16.Rows)
                    {
                        double ampacity;
                        if (!double.TryParse(linha["60"].ToString(), out ampacity))
                            continue;

                        if (ampacity >= currentPerCable)
                        {
                            paparam.cableUnder = $"{cablesPerPhase} - {linha["size"]}";
                            lblCableUnder.Content = "Cable Unde: " + paparam.cableUnder;
                            return; // encontrou, sai do método
                        }
                    }

                    // Se não encontrou nenhum cabo que suporte a corrente atual por cabo,
                    // aumenta o número de cabos por fase e tenta novamente
                    cablesPerPhase++;
                    currentPerCable = currentUsed / cablesPerPhase;

                    // Limite de segurança para evitar loop infinito
                    if (cablesPerPhase > 20)
                    {
                        throw new Exception("Não foi possível encontrar um cabo adequado mesmo com até 20 cabos por fase.");
                    }
                }

            }
            else
            {
                int cablesPerPhase = 1;

                double currentPerCable = currentUsed;

                while (true)
                {
                    foreach (DataRow linha in nec_310_16.Rows)
                    {
                        double ampacity;
                        if (!double.TryParse(linha["75"].ToString(), out ampacity))
                            continue;

                        if (ampacity >= currentPerCable)
                        {
                            paparam.cableUnder = $"{cablesPerPhase} - {linha["size"]}";
                            lblCableUnder.Content = "Cable Under: " + paparam.cableUnder;
                            return; // encontrou, sai do método
                        }
                    }

                    // Se não encontrou nenhum cabo que suporte a corrente atual por cabo,
                    // aumenta o número de cabos por fase e tenta novamente
                    cablesPerPhase++;
                    currentPerCable = currentUsed / cablesPerPhase;

                    // Limite de segurança para evitar loop infinito
                    if (cablesPerPhase > 20)
                    {
                        throw new Exception("Não foi possível encontrar um cabo adequado mesmo com até 10 cabos por fase.");
                    }
                }

            }

        }

        private void cmbUnitMV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUnitMV.SelectedValue == "HP" || cmbUnitMV.SelectedValue == "kW")
            {
                cmbLoadTypeMV.Items.Clear();
                cmbLoadTypeMV.Items.Add("MOTOR");

            }
            else
            {
                cmbLoadTypeMV.Items.Clear();
                cmbLoadTypeMV.Items.Add("XMRFS");
            }

            paparam.MultiplierFactorMV = 1.25;
        }

        private void btnStep1MV_Click(object sender, RoutedEventArgs e)
        {
            paparam.powerFactorMV = Convert.ToDouble(txtPFMV.Text);

            paparam.voltageLevelMV = Convert.ToDouble(txtVoltageMV.Text);

            paparam.tempAmbienteMV = txtAmbTempMV.Text == "" ? 40 : Convert.ToDouble(txtAmbTempMV.Text);

            paparam.deRatingTempFactorMV = Math.Sqrt((105 - paparam.tempAmbienteMV) / (105 - 40));

            double multiplierHP = cmbUnitMV.SelectedValue == "HP" ? 0.746 : 1; // Convert HP to kW if needed

            if (cmbLoadTypeMV.SelectedValue == "MOTOR")
            {
                paparam.InomMV = Convert.ToDouble(txt_Power_MV.Text) * multiplierHP * 1000 / (paparam.voltageLevelMV * Math.Sqrt(3) * paparam.powerFactorMV);

                paparam.IA2MV = paparam.InomMV * paparam.deRatingTempFactorMV * paparam.MultiplierFactorMV;

            }
            else
            {
                paparam.InomMV = Convert.ToDouble(txt_Power_MV.Text) * 1000 / (paparam.voltageLevelMV * Math.Sqrt(3));
                paparam.IA2MV = paparam.InomMV * paparam.deRatingTempFactorMV * paparam.MultiplierFactorMV;
            }

            lblINomMV.Content = "Inom: " + paparam.InomMV.ToString("F2") + " A";

            lblIA2MV.Content = "New Current: " + paparam.IA2MV.ToString("F2") + " A";
        }

        private void btnStep2MVAbove_Click(object sender, RoutedEventArgs e)
        {

            DataTable nec_315_60 = acessos.ExecuteQuery("SELECT size, a_90, a_105, a_mv90, a_mv105 FROM NEC_315_60");

            double currentUsed = paparam.IA2MV / Convert.ToDouble(txtAboveDeratingMV.Text);

            int colunaDeBusca = paparam.voltageLevelMV > 5000 ? 1 : 3;

            int cablesPerPhase = 1;

            double currentPerCable = currentUsed;

            while (true)
            {
                foreach (DataRow linha in nec_315_60.Rows)
                {
                    double ampacity;
                    if (!double.TryParse(linha[colunaDeBusca].ToString(), out ampacity))
                        continue;

                    if (ampacity >= currentPerCable)
                    {
                        paparam.cableAboveMV = $"{cablesPerPhase} - {linha["size"]}";
                        lblAboveCableMV.Content = "Cable Above: " + paparam.cableAboveMV;
                        return; // encontrou, sai do método
                    }
                }

                // Se não encontrou nenhum cabo que suporte a corrente atual por cabo,
                // aumenta o número de cabos por fase e tenta novamente
                cablesPerPhase++;
                currentPerCable = currentUsed / cablesPerPhase;

                // Limite de segurança para evitar loop infinito
                if (cablesPerPhase > 20)
                {
                    throw new Exception("Não foi possível encontrar um cabo adequado mesmo com até 20 cabos por fase.");
                }
            }



        }

        private void btnStep3MV_Click(object sender, RoutedEventArgs e)
        {
            int column = 0;

            column = (underMVDet1.IsChecked == true) ? 1 : column;
            column = (underMVDet2.IsChecked == true) ? 2 : column;
            column = (underMVDet3.IsChecked == true) ? 3 : column;

            string high = (paparam.voltageLevelMV > 5000) ? "h" : "";

            DataTable nec_315_60 = acessos.ExecuteQuery($"SELECT size, \"{column.ToString()}_mv{high}90\"  FROM NEC_315_60");

            double currentUsed = paparam.IA2MV / Convert.ToDouble(txtUnderDeratingMV.Text);

            
            int cablesPerPhase = 1;

            double currentPerCable = currentUsed;

            while (true)
            {
                foreach (DataRow linha in nec_315_60.Rows)
                {
                    double ampacity;
                    if (!double.TryParse(linha[1].ToString(), out ampacity))
                        continue;

                    if (ampacity >= currentPerCable)
                    {
                        paparam.cableUnderMV = $"{cablesPerPhase} - {linha["size"]}";
                        lblUnderCableMV.Content = "Cable Under: " + paparam.cableUnderMV;
                        return; // encontrou, sai do método
                    }
                }

                // Se não encontrou nenhum cabo que suporte a corrente atual por cabo,
                // aumenta o número de cabos por fase e tenta novamente
                cablesPerPhase++;
                currentPerCable = currentUsed / cablesPerPhase;

                // Limite de segurança para evitar loop infinito
                if (cablesPerPhase > 20)
                {
                    throw new Exception("Não foi possível encontrar um cabo adequado mesmo com até 20 cabos por fase.");
                }
            }

        }
    }
}

