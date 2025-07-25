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
using System.Security.Cryptography;
using System.IO;
using static MaterialDesignThemes.Wpf.Theme;
using System.Diagnostics;
using MaterialDesignThemes.Wpf;
using System.Windows.Media.Animation;

namespace WpfApp2.Neher
{
    /// <summary>
    /// Interaction logic for Neher.xaml
    /// </summary>
    public partial class Neher : UserControl
    {

        DatabaseAccess acessos = new();

        NeherElement elementoNeher = new();

        NeherCalc calc = new();
        Cable cable = new();
        public Neher()
        {

            InitializeComponent();
            CriarGradeNumerica(8, 8);

            MySnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
        }

        private System.Windows.Controls.TextBox ultimoTextBoxComFoco;
        private Border ultimoBorderDestacado;

        private void CriarGradeNumerica(int rows, int columns)
        {
            InputGrid.Rows = rows;
            InputGrid.Columns = columns;
            InputGrid.Children.Clear(); // opcional: limpa antes

            for (int i = 0; i < rows * columns; i++)
            {
                var textBox = new System.Windows.Controls.TextBox
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // ➕ Adiciona o evento GotFocus
                textBox.GotFocus += TextBox_GotFocus;

                var border = new Border
                {
                    BorderThickness = new Thickness(0.5),
                    BorderBrush = Brushes.Black,
                    Child = textBox,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                InputGrid.Children.Add(border);
            }
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.TextBox;
            if (tb == null) return;

            // Remove destaque anterior
            if (ultimoBorderDestacado != null)
            {
                ultimoBorderDestacado.BorderBrush = Brushes.Black;
                ultimoBorderDestacado.Background = Brushes.White;
            }

            // Acha o novo Border pai
            var novoBorder = VisualTreeHelper.GetParent(tb) as Border;
            if (novoBorder != null)
            {
                novoBorder.BorderBrush = Brushes.Red; // ou outra cor de destaque
                novoBorder.Background = Brushes.LightYellow;

                ultimoBorderDestacado = novoBorder;
            }

            // Salva o último TextBox com foco
            ultimoTextBoxComFoco = tb;
        }



        private string[,] ObterValoresPreenchidosDoGrid()
        {
            int rows = InputGrid.Rows;
            int columns = InputGrid.Columns;

            string[,] temp = new string[rows, columns];
            int index = 0;

            int maxRow = -1;
            int maxCol = -1;

            // Primeiro passo: armazenar tudo e descobrir o maior índice preenchido
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    string value = "";
                    if (InputGrid.Children[index] is Border border && border.Child is System.Windows.Controls.TextBox tb)
                    {
                        value = tb.Text.Trim();
                        temp[i, j] = value;

                        if (!string.IsNullOrEmpty(value))
                        {
                            if (i > maxRow) maxRow = i;
                            if (j > maxCol) maxCol = j;
                        }
                    }

                    index++;
                }
            }

            // Se nada foi preenchido, retorna matriz vazia
            if (maxRow == -1 || maxCol == -1)
                return new string[0, 0];

            // Criar matriz do tamanho necessário
            string[,] resultado = new string[maxRow + 1, maxCol + 1];

            for (int i = 0; i <= maxRow; i++)
            {
                for (int j = 0; j <= maxCol; j++)
                {
                    resultado[i, j] = temp[i, j];
                }
            }

            return resultado;
        }

        private void cmbTBase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        // load cables according voltage level
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double voltageLevel = Convert.ToDouble(txtVoltageMain.Text) / 1000;
            DataTable retorno = new DataTable();
            cmbTemp.Items.Clear();
            if (voltageLevel >= 2)
            {
                retorno = acessos.ExecuteQuery($"SELECT size FROM medium_voltage");
                cmbTemp.Items.Add("90");
                cmbTemp.Items.Add("105");
            }
            else
            {
                retorno = acessos.ExecuteQuery($"SELECT size FROM low_voltage");
                cmbTemp.Items.Add("75");
                cmbTemp.Items.Add("90");
            }

            List<string> cables = retorno.AsEnumerable()
                              .Select(row => row["size"].ToString())
                              .ToList();

            cmbCables.ItemsSource = cables;

            retorno = acessos.ExecuteQuery("Select Size from conduitsNeher");

            List<string> conduits = retorno.AsEnumerable()
                              .Select(row => row["Size"].ToString())
                              .ToList();

            MySnackbar.MessageQueue?.Enqueue("Loaded Successfully");

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in InputGrid.Children)
            {
                if (child is Border border && border.Child is System.Windows.Controls.TextBox tb)
                {
                    tb.Text = string.Empty;
                }
            }
        }

      
        private void cmbVoltageLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVoltageLevel.SelectedIndex == 0)
            {
                cmbTc.Items.Clear();
                cmbTc.Items.Add("60");
                cmbTc.Items.Add("75");
                cmbTc.Items.Add("90");

                cmbTclinha.Items.Clear();
                cmbTclinha.Items.Add("60");
                cmbTclinha.Items.Add("75");
                cmbTclinha.Items.Add("90");

                txtTa.Text = 30.ToString();
            }

            else
            {
                cmbTc.Items.Clear();
                cmbTc.Items.Add("90");
                cmbTc.Items.Add("105");

                cmbTclinha.Items.Clear();
                cmbTclinha.Items.Add("75");
                cmbTclinha.Items.Add("90");
                cmbTclinha.Items.Add("105");

                txtTa.Text = 20.ToString();
            }



        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            double tc = Convert.ToDouble(cmbTc.SelectedValue);
            double tcLinha = Convert.ToDouble(cmbTclinha.SelectedValue);
            double ta = Convert.ToDouble(txtTa.Text);

            double taLinha = Convert.ToDouble(txtTalinha.Text);

            double ft = Math.Sqrt(((tcLinha - ta) / (tc - ta)) * ((234.5 + tc) / (234.5 + tcLinha)));

            string sizeAprox = cmbSizeAprox.Text.ToString();

            string numbOfCKT = cmbNumCkt.Text.ToString();

            double rhoSoil = Convert.ToDouble(cmbRho.Text);

            string tableToSearch;
            string ieeToSearch;
            if (cmbVoltageLevel.SelectedIndex == 0)
            {
                tableToSearch = "fth_lv";
                ieeToSearch = "iee_13_8";
            }
            else
            {
                tableToSearch = "fth_mv";
                ieeToSearch = "iee_13_9";
            }

            string deRetorno = $"SELECT [{cmbRho.Text.ToString()}]  FROM {tableToSearch} WHERE size = '{sizeAprox}' AND numberOfCKT = '{numbOfCKT}'";
            DataTable retorno = acessos.ExecuteQuery(deRetorno);

            double fth = Convert.ToDouble(retorno.Rows[0][0]);


            string numberOfRows = cmbRows.Text.ToString();
            string numberOfColumns = cmbCols.Text.ToString();

            string sizeIee = cmbCableSizeIEE.Text.ToString();

            retorno = acessos.ExecuteQuery($"SELECT [{numberOfColumns}] FROM {ieeToSearch} WHERE Cable_size = '{sizeIee}' AND No_of_rows = '{numberOfRows}'");

            double fg = Convert.ToDouble(retorno.Rows[0][0]);

            double newF = ft * fth * fg;

            string buscaCorrente;

            if (cmbVoltageLevel.SelectedIndex == 0)
            {
                buscaCorrente = $"SELECT [{cmbTc.Text.ToString()}] FROM nec_310_16 WHERE size = '{sizeIee}'";
            }
            else if (cmbVoltageLevel.SelectedIndex == 1)
            {
                buscaCorrente = $"SELECT [1_mv{cmbTc.Text.ToString()}] FROM nec_315_60 WHERE size = '{sizeIee}'";
            }
            else
            {
                buscaCorrente = $"SELECT [1_mvh{cmbTc.Text.ToString()}] FROM nec_315_60 WHERE size = '{sizeIee}'";
            }

            double correnteBuscada = Convert.ToDouble(acessos.ExecuteQuery(buscaCorrente).Rows[0][0]);

            double correnteCalculada = correnteBuscada * newF;


            lblIBuscada.Content = "I = " + correnteBuscada.ToString("F2") + "A";
            lblICalculada.Content = "I' = " + correnteCalculada.ToString("F2") + "A";

        }

        private void cmbLoadTypeMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            cmbCurrentFactor.SelectedIndex = 1;
            int loadType = cmbLoadTypeMain.SelectedIndex;

            double voltage = Convert.ToDouble(txtVoltageMain.Text);

            string carga = "";

            lblAuxHP.Visibility = Visibility.Hidden;
            lblAuxPower.Visibility = Visibility.Hidden;
            cmbAuxPower.Visibility = Visibility.Hidden;


            cmbAuxPower.ItemsSource = null;
            cmbUnitsMain.ItemsSource = null;

            switch (loadType)
            {

                case 0:

                    carga = "MOTOR";

                    if (voltage > 2000)
                    {

                        lblPowerMain.Visibility = Visibility.Visible;
                        txtPowerMain.Visibility = Visibility.Visible;
                        cmbUnitsMain.Visibility = Visibility.Visible;
                        cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                        cmbUnitsMain.SelectedIndex = 0;
                        lblPowerFactorMain.Visibility = Visibility.Visible;
                        txtPowerFactorMain.Visibility = Visibility.Visible;
                        lblEfficiencyMain.Visibility = Visibility.Visible;
                        txtEfficiencyMain.Visibility = Visibility.Visible;

                        lblPowerMain.IsEnabled = true;
                        txtPowerMain.IsEnabled = true;
                        cmbUnitsMain.IsEnabled = true;
                        cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                        lblPowerFactorMain.IsEnabled = true;
                        txtPowerFactorMain.IsEnabled = true;
                        txtEfficiencyMain.IsEnabled = true;
                        lblEfficiencyMain.IsEnabled = true;

                    }
                    else
                    {
                        lblPowerMain.Visibility = Visibility.Hidden;
                        txtPowerMain.Visibility = Visibility.Hidden;
                        cmbUnitsMain.Visibility = Visibility.Hidden;


                        lblAuxHP.Visibility = Visibility.Visible;
                        lblAuxPower.Visibility = Visibility.Visible;
                        cmbAuxPower.Visibility = Visibility.Visible;
                        cmbAuxPower.DisplayMemberPath = "Horsepower";
                        cmbAuxPower.ItemsSource = acessos.ExecuteQuery("SELECT Horsepower from nec_430_250").DefaultView;

                        lblPowerFactorMain.Visibility = Visibility.Hidden;
                        txtPowerFactorMain.Visibility = Visibility.Hidden;
                        lblEfficiencyMain.Visibility = Visibility.Hidden;
                        txtEfficiencyMain.Visibility = Visibility.Hidden;


                    }

                    break;


                case 1:
                    carga = "XFRM";
                    cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                    lblPowerMain.Visibility = Visibility.Visible;
                    txtPowerMain.Visibility = Visibility.Visible;
                    cmbUnitsMain.Visibility = Visibility.Visible;
                    lblPowerMain.IsEnabled = true;
                    txtPowerMain.IsEnabled = true;
                    cmbUnitsMain.IsEnabled = true;
                    cmbUnitsMain.SelectedIndex = 0;

                    lblPowerFactorMain.Visibility = Visibility.Hidden;
                    txtPowerFactorMain.Visibility = Visibility.Hidden;
                    lblEfficiencyMain.Visibility = Visibility.Hidden;
                    txtEfficiencyMain.Visibility = Visibility.Hidden;

                    break;
                case 2:
                    carga = "HEATER";
                    cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                    lblPowerMain.Visibility = Visibility.Visible;
                    txtPowerMain.Visibility = Visibility.Visible;
                    cmbUnitsMain.Visibility = Visibility.Visible;
                    lblPowerMain.IsEnabled = true;
                    txtPowerMain.IsEnabled = true;
                    cmbUnitsMain.IsEnabled = true;
                    cmbUnitsMain.SelectedIndex = 0;


                    lblPowerFactorMain.Visibility = Visibility.Hidden;
                    txtPowerFactorMain.Visibility = Visibility.Hidden;
                    lblEfficiencyMain.Visibility = Visibility.Hidden;
                    txtEfficiencyMain.Visibility = Visibility.Hidden;
                    break;
                case 3:
                    carga = "FEEDER";
                    cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                    lblPowerMain.Visibility = Visibility.Visible;
                    txtPowerMain.Visibility = Visibility.Visible;
                    cmbUnitsMain.Visibility = Visibility.Visible;
                    lblPowerMain.IsEnabled = true;
                    txtPowerMain.IsEnabled = true;
                    cmbUnitsMain.IsEnabled = true;
                    cmbUnitsMain.SelectedIndex = 0;

                    lblPowerFactorMain.Visibility = Visibility.Hidden;
                    txtPowerFactorMain.Visibility = Visibility.Hidden;
                    lblEfficiencyMain.Visibility = Visibility.Hidden;
                    txtEfficiencyMain.Visibility = Visibility.Hidden;
                    break;
                case 4:
                    carga = "GENERATOR";
                    lblPowerMain.Visibility = Visibility.Visible;
                    txtPowerMain.Visibility = Visibility.Visible;
                    cmbUnitsMain.Visibility = Visibility.Visible;
                    cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                    cmbUnitsMain.SelectedIndex = 0;
                    lblPowerFactorMain.Visibility = Visibility.Visible;
                    txtPowerFactorMain.Visibility = Visibility.Visible;
                    lblEfficiencyMain.Visibility = Visibility.Visible;
                    txtEfficiencyMain.Visibility = Visibility.Visible;

                    lblPowerMain.IsEnabled = true;
                    txtPowerMain.IsEnabled = true;
                    cmbUnitsMain.IsEnabled = true;
                    cmbUnitsMain.ItemsSource = calc.retornaUnidades(carga);
                    lblPowerFactorMain.IsEnabled = true;
                    txtPowerFactorMain.IsEnabled = true;
                    txtEfficiencyMain.IsEnabled = true;
                    lblEfficiencyMain.IsEnabled = true;
                    break;
                default:

                    break;
            }
        }

        private void cmbAuxPower_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sizedCurrentStr = "";

            double sizedCurrent = 0;
            if (cmbAuxPower.SelectedItem == null)
            {
                Debug.WriteLine("SelectedItem está null.");
                return;
            }

            Debug.WriteLine($"Tipo de SelectedItem: {cmbAuxPower.SelectedItem.GetType()}");

            if (cmbAuxPower.SelectedItem is DataRowView row)
            {
                string selecao = row["Horsepower"].ToString();

                sizedCurrentStr = acessos.ExecuteQuery($"SELECT current FROM nec_430_250 WHERE Horsepower = '{selecao}'").Rows[0][0].ToString();
            }

            sizedCurrent = Convert.ToDouble(sizedCurrentStr) * Convert.ToDouble(cmbCurrentFactor.Text);

            txtCurrentMain.Text = sizedCurrent.ToString("F2");

        }

        private void btnCurrentMain_Click(object sender, RoutedEventArgs e)
        {
            string powerUnit = cmbUnitsMain.Text;

            double powerFactor = txtPowerFactorMain.IsEnabled ? Convert.ToDouble(txtPowerFactorMain.Text) : 1;
            double efficiency = txtEfficiencyMain.IsEnabled ? Convert.ToDouble(txtEfficiencyMain.Text) : 1;

            double sizedCurrent = 0;

            switch (powerUnit)
            {
                case "kW":
                    sizedCurrent = Convert.ToDouble(txtPowerMain.Text) * 1000 / (Math.Sqrt(3) * Convert.ToDouble(txtVoltageMain.Text) * powerFactor * efficiency);
                    break;
                case "HP":
                    sizedCurrent = Convert.ToDouble(txtPowerMain.Text) * 0.7456 * 1000 / (Math.Sqrt(3) * Convert.ToDouble(txtVoltageMain.Text) * powerFactor * efficiency);
                    break;

                case "kVA":
                    sizedCurrent = Convert.ToDouble(txtPowerMain.Text) * 1000 / (Math.Sqrt(3) * Convert.ToDouble(txtVoltageMain.Text));
                    break;

            }

            sizedCurrent = sizedCurrent * Convert.ToDouble(cmbCurrentFactor.Text);

            txtCurrentMain.Text = sizedCurrent.ToString("F2");
        }

        private void btnCablePhase_Click(object sender, RoutedEventArgs e)
        {
            double currentNow = Convert.ToDouble(txtCurrentMain.Text);

            int multiplicador = Convert.ToInt16(btnCablePhase.Content.ToString().Split('-')[0]) / 3;

            multiplicador++;

            double currentPhase = currentNow / multiplicador;

            txtCurrentLinha.Text = currentPhase.ToString("F2");


            btnCablePhase.Content = $"{multiplicador * 3}-1/C";


        }

        private void btnClearMain_Click(object sender, RoutedEventArgs e)
        {
            btnCablePhase.Content = "3-1/C";

            cmbLoadTypeMain.SelectedIndex = -1;
            txtCurrentLinha.Text = null;
            txtCurrentMain.Text = null;
        }

        /// <summary>
        ///  Bloco IEE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            StringBuilder log = new StringBuilder();
            string[,] matriz = ObterValoresPreenchidosDoGrid();  // seu método

            cableNew cnew = new cableNew();

            // Inputs exemplo (substitua por UI)
            cnew.voltageMain = Convert.ToDouble(txtVoltageMain.Text);
            cnew.cableSection = cmbCables.SelectedValue.ToString();
            cnew.soilTemp = Convert.ToDouble(txtSoilTemp.Text);
            cnew.soilRes_m = Convert.ToDouble(txtRhoSoil.Text) / 100;  // °C.cm/W → K.m/W

            double profundidadeEnterramento_m = Convert.ToDouble(txtH.Text) / 39.37;

            cnew.theta = Convert.ToDouble(cmbTemp.Text); // guess inicial

            // Resistencia AC (correto)
            string tabelaDeBusca = cnew.voltageMain > 2000 ? "medium_voltage" : "low_voltage";
            DataTable retorno = acessos.ExecuteQuery($"SELECT rdc_25 FROM {tabelaDeBusca} WHERE size = '{cnew.cableSection}'");
            cnew.Rdc_25_ohm_1000ft = Convert.ToDouble(retorno.Rows[0][0]);
            cnew.Rdc_25_ohm_m = cnew.Rdc_25_ohm_1000ft / 304.8;
            cnew.R0 = cnew.Rdc_25_ohm_m / 1.020;

            
            cnew.Rlinha = cnew.R0 * (1 + (0.00393 * (cnew.theta - cnew.soilTemp)));
            cnew.xs2 = 8 * Math.PI * 60 * Math.Pow(10, -7) / cnew.Rlinha;
            cnew.ys = Math.Pow(cnew.xs2, 2) / (192 + 0.8 * Math.Pow(cnew.xs2, 2));

            retorno = acessos.ExecuteQuery($"Select dim_bare, OD from {tabelaDeBusca} where size = '{cnew.cableSection}'");
            cnew.diam_in_in = Convert.ToDouble(retorno.Rows[0][0]);
            cnew.diam_in_mm = cnew.diam_in_in * 25.4;
            cnew.diam_ext_in = Convert.ToDouble(retorno.Rows[0][1]);
            cnew.diam_ext_mm = cnew.diam_ext_in * 25.4;


            // Para trifolio, s_mm approx = diam_ext_mm * (2 / Math.Sqrt(3)) for touching
            double s_mm = cnew.diam_ext_mm * (2 / Math.Sqrt(3));

            cnew.yp = calcYp(cnew.diam_in_mm, s_mm, cnew.Rlinha);  // ajuste para trifolio

            cnew.R = cnew.Rlinha * (1 + cnew.ys + cnew.yp);


            // T1 (correto)
            if (cnew.voltageMain < 2000)
            {
                retorno = acessos.ExecuteQuery($"select insul from low_voltage where size = '{cnew.cableSection}'");
                cnew.t1_inches = Convert.ToDouble(retorno.Rows[0][0]) / 1000;
                cnew.t1_mm = cnew.t1_inches * 25.4;
            }
            else
            {
                retorno = acessos.ExecuteQuery($"select dim_bare, dim_over_insul from medium_voltage where size = '{cnew.cableSection}'");
                cnew.t1_inches = Convert.ToDouble(retorno.Rows[0][1]) - Convert.ToDouble(retorno.Rows[0][0]);
                cnew.t1_mm = cnew.t1_inches * 25.4;
            }

            double rhoT = 3.5;
            cnew.T1 = (rhoT / (2 * Math.PI)) * Math.Log(1 + (2 * cnew.t1_mm / cnew.diam_in_mm));



            // Calcular centers e T4 no loop
            int rows = matriz.GetLength(0);
            int cols = matriz.GetLength(1);
            double polegadaParaMetro = 0.0254;
            double espacamentoBorda_m = 3.0 * polegadaParaMetro;

            double[,] raioDuto = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    raioDuto[i, j] = double.Parse(matriz[i, j].Replace("\"", "").Trim()) * polegadaParaMetro / 2.0;
                }
            }

            double[] centerX = new double[cols];
            if (cols > 0)
            {
                double maxRaioCol0 = 0;
                for (int i = 0; i < rows; i++) maxRaioCol0 = Math.Max(maxRaioCol0, raioDuto[i, 0]);
                centerX[0] = maxRaioCol0 + espacamentoBorda_m;
                for (int j = 1; j < cols; j++)
                {
                    double maxSum = 0;
                    for (int i = 0; i < rows; i++)
                    {
                        double sum = raioDuto[i, j - 1] + raioDuto[i, j];
                        if (sum > maxSum) maxSum = sum;
                    }
                    centerX[j] = centerX[j - 1] + maxSum + 3.0 * polegadaParaMetro;
                }
            }

            double[] centerY = new double[rows];
            if (rows > 0)
            {
                double maxRaioLinha0 = 0;
                for (int j = 0; j < cols; j++) maxRaioLinha0 = Math.Max(maxRaioLinha0, raioDuto[0, j]);
                centerY[0] = maxRaioLinha0 + espacamentoBorda_m;
                for (int i = 1; i < rows; i++)
                {
                    double maxSum = 0;
                    for (int j = 0; j < cols; j++)
                    {
                        double sum = raioDuto[i - 1, j] + raioDuto[i, j];
                        if (sum > maxSum) maxSum = sum;
                    }
                    centerY[i] = centerY[i - 1] + maxSum + 3.0 * polegadaParaMetro;
                }
            }

            // Loop de iteração para I
            double n = 3.0;  // trifolio
            double I = 500.0;  // guess inicial
            double deltaTheta = cnew.theta - cnew.soilTemp;
            int iter = 0;
            double I_old = double.MaxValue;

            #region log
            log.AppendLine($"Voltage : {cnew.voltageMain}");
            log.AppendLine($"Cable Section : {cnew.cableSection}");
            log.AppendLine($"Soil Temperature : {cnew.soilTemp}");
            log.AppendLine($"Soil Resistivity : {cnew.soilRes_m} K.m/W");
            log.AppendLine($"Profundidade Enterramento : {profundidadeEnterramento_m} m");
            log.AppendLine($"Rdc_25_ohm_1000ft : {cnew.Rdc_25_ohm_1000ft} ohm/1000ft");
            log.AppendLine($"Rdc_25_ohm_m : {cnew.Rdc_25_ohm_m} ohm/m");
            log.AppendLine($"R0 : {cnew.R0} ohm/m");
            log.AppendLine($"Rlinha : {cnew.Rlinha} ohm/m");
            log.AppendLine($"xs2 : {cnew.xs2} ohm/m");
            log.AppendLine($"ys : {cnew.ys} ohm/m");
            log.AppendLine($"Diametro Interno (mm): {cnew.diam_in_mm}");
            log.AppendLine($"Diametro Externo (mm): {cnew.diam_ext_mm}");
            log.AppendLine($"Diametro interno (in): {cnew.diam_in_in}");
            log.AppendLine($"Diametro externo (in): {cnew.diam_ext_in}");
            log.AppendLine($"Yp : {cnew.yp} ohm/m");
            log.AppendLine($"R : {cnew.R} ");
            log.AppendLine($"T1 : {cnew.T1} K.m/W");
            log.AppendLine($"rhoT : {rhoT} K.m/W");

            #endregion

            while (Math.Abs(I - I_old) > 0.1 && iter < 50)
            {
                I_old = I;

                // Recalcula T4 com a temperatura média atual
                double T4 = CalcularT4(cnew, matriz, profundidadeEnterramento_m, rows, cols, centerX, centerY);

                // Resistência térmica total (use o fator m adequado, por exemplo 2 ou 3)
                double R_th_total = cnew.T1 + n * T4;

                // Calcula corrente com a resistência AC atual
                I = Math.Sqrt(deltaTheta / (cnew.R * R_th_total));

                // Temperatura final do condutor
                double deltaTheta_new = I * I * cnew.R * R_th_total;
                cnew.theta = cnew.soilTemp + deltaTheta_new;

                // *** Atualiza a resistência AC com base na nova temperatura ***
                // R0 já foi calculado antes (resistência a 20 °C)
                cnew.Rlinha = cnew.R0 * (1 + 0.00393 * (cnew.theta - cnew.soilTemp));

                // Efeito de pelicularidade e de proximidade:
                cnew.xs2 = 8 * Math.PI * 60.0 * Math.Pow(10, -7) / cnew.Rlinha;
                cnew.ys = Math.Pow(cnew.xs2, 2) / (192.0 + 0.8 * Math.Pow(cnew.xs2, 2));

                // Cálculo de yp depende da separação entre os cabos (s_mm); 
                
                cnew.yp = calcYp(cnew.diam_in_mm, s_mm, cnew.Rlinha);

                // Resistência AC atualizada:
                cnew.R = cnew.Rlinha * (1 + cnew.ys + cnew.yp);

                // Atualiza deltaTheta para a próxima iteração
                deltaTheta = deltaTheta_new;

                iter++;

                #region log

                log.AppendLine($"\n\n--- Iteration {iter + 1} ---");
                log.AppendLine($"Current guess I : {I} A");
                log.AppendLine($"Delta Theta : {deltaTheta} K");
                log.AppendLine($"Theta : {cnew.theta} K");

                #endregion
            }

            // Saída
            cnew.ampacidade = I;
            txttrash.Text = cnew.ampacidade.ToString("F2");

            #region log
            string caminho = @"C:\temp\log_code.txt";

            try
            {
                File.WriteAllText(caminho, log.ToString());
                
            }
            catch (Exception ex)
            {
            }

            #endregion

        }

        public static double calcYp(double dc_mm, double s_mm, double Rlinha)
        {
            // Constantes
            const double pi = Math.PI;
            const double f = 60; // Hz
            const double kp = 1.0;

            // Cálculo de xp^2 (IEC)
            double xp2 = (8 * pi * f / Rlinha) * 1e-7 * kp;

            // Cálculo de xp^4
            double xp4 = Math.Pow(xp2, 2);

            // Cálculo da razão (dc / s)
            double ratio = dc_mm / s_mm;
            double ratio2 = ratio * ratio;

            // Fração auxiliar da IEC
            double fracIEC = xp4 / (192 + 0.8 * xp4);

            // Cálculo de y_p com base na IEC
            double yp = fracIEC * ratio2 *
                        (0.312 * ratio2 + (1.18 / (fracIEC + 0.27)));



            return yp;
        }

        public double CalcularT4(cableNew cnew, string[,] matriz, double profundidade_m, int rows, int cols, double[] centerX, double[] centerY)
        {
            // Constantes para T4' (ar em PVC, IEC Tabela 4)
            double U = 1.87;
            double V = 0.312;
            double Y = 0.0037;

            // rho para duto PVC
            double rho_duto = Convert.ToDouble(txtRhoDuct.Text)/100;

            // rho solo
            double rho_solo = cnew.soilRes_m;  // já em K.m/W

            // theta_m médio no gap (usa theta atual do condutor)
            double theta_m = (cnew.theta + cnew.soilTemp) / 2.0;

            // De_mm = diam_ext_mm (diâmetro cabo, mm)
            double De_mm = cnew.diam_ext_mm;

            // T4' (gap ar)
            double T4_prime = U / (1 + 0.1 * (V + Y * theta_m) * De_mm);

            // Identificar hottest duto (centro)
            int i_hot = hottestx;
            int j_hot = hottesty;

            // Parse Do e Di para hottest (assuma matriz tem Di em inches; fetch Do de DB)
            string size_hot = matriz[i_hot, j_hot].Replace("\"", "").Trim();
            DataTable retorno = acessos.ExecuteQuery($"Select Average_OD_in from conduitsNeher where Size = '{size_hot}'");

            double diametroIntDuto_in = double.Parse(size_hot);  // Di de matriz
            double diametroExtDuto_in = Convert.ToDouble(retorno.Rows[0][0]);  // Do de DB

            double Di_m = diametroIntDuto_in * 0.0254;
            double Do_m = diametroExtDuto_in * 0.0254;

            // T4'' (parede duto)
            double T4_double = (rho_duto / (2 * Math.PI)) * Math.Log(Do_m / Di_m);

            // T4''' (solo com images)
            double L_k = profundidade_m + centerY[i_hot];  // profundidade ao centro do hottest

            double sum_logs = 0.0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (i == i_hot && j == j_hot) continue;

                    double dx = centerX[j] - centerX[j_hot];
                    double dy = centerY[i] - centerY[i_hot];
                    double d_kj = Math.Sqrt(dx * dx + dy * dy);
                    if (d_kj == 0) continue;

                    double L_j = profundidade_m + centerY[i];
                    double d_kj_prime = Math.Sqrt(d_kj * d_kj + (L_k + L_j) * (L_k + L_j));

                    sum_logs += Math.Log(d_kj_prime / d_kj);
                }
            }

            double term_isol = Math.Log(4 * L_k / Do_m);
            double T4_triple = (rho_solo / (2 * Math.PI)) * (term_isol + sum_logs);

            // T4 total
            return T4_prime + T4_double + T4_triple;
        }

        int hottestx = 0;
        int hottesty = 0;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ultimoTextBoxComFoco == null)
            {
                MessageBox.Show("Nenhuma célula foi selecionada.");
                return;
            }

            int rows = InputGrid.Rows;
            int columns = InputGrid.Columns;

            for (int index = 0; index < InputGrid.Children.Count; index++)
            {
                if (InputGrid.Children[index] is Border border && border.Child == ultimoTextBoxComFoco)
                {
                    int row = index / columns;
                    int col = index % columns;

                    hottestx = row;
                    hottesty = col;

                    Button_Click_3(sender, e);

                    return;
                }
            }


        }
    }
}