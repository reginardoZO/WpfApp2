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


        }

        private void CriarGradeNumerica(int rows, int columns)
        {
            InputGrid.Rows = rows;
            InputGrid.Columns = columns;

            for (int i = 0; i < rows * columns; i++)
            {
                var textBox = new TextBox
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    BorderThickness = new Thickness(0), // sem borda interna
                };

                // Faz o TextBox crescer dentro do Border
                textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                textBox.VerticalAlignment = VerticalAlignment.Stretch;

                // Permitir apenas números


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




        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //load factor
            cable.LF = Convert.ToDouble(txtLF.Text) / 100;

            cable.voltage = Convert.ToDouble(txtVoltageLevel.Text);

            cable.soilTemp = Convert.ToDouble(txtSoilTemp.Text);

            double e_r = 0;
            double cosFi = 0;

            string tableToSearch = "";
            if (cable.voltage >= 2)
            {
                tableToSearch = "medium_voltage";
                e_r = 3;
                cosFi = 0.002;
            }
            else
            {
                e_r = 4;
                cosFi = 0.05;
                tableToSearch = "low_voltage";
            }

            cable.TC = Convert.ToDouble(cmbTemp.Text);

            cable.size = cmbCables.SelectedValue.ToString();

            DataTable retorno = acessos.ExecuteQuery($"SELECT rdc_25 FROM {tableToSearch} WHERE size = '{cable.size}'");

            cable.Rdc_25C = Convert.ToDouble(retorno.Rows[0][0]);

            cable.Rdc_TC = cable.Rdc_25C * (1 + 0.00393 * (cable.TC - 25));

            cable.Rdc_TC_ft = cable.Rdc_TC / 1000;

            double mu = 4 * Math.PI * 1e-7;
            double freq = 60.0;
            double Rdc_m = cable.Rdc_TC_ft / 0.3048; // converter para ohm/m
            cable.Xs = (2 * Math.PI * freq * mu) / Rdc_m;

            cable.Y_skin = Math.Pow(cable.Xs, 4) / (192 + 0.8 * Math.Pow(cable.Xs, 4));

            retorno = acessos.ExecuteQuery($"SELECT dim_bare FROM {tableToSearch} WHERE size = '{cable.size}'");

            cable.d_c = Convert.ToDouble(retorno.Rows[0][0]) * 0.0254; // polegadas → metros


            if (cable.voltage >= 2)
            {
                // dim_over_insul já é o diâmetro total em polegadas → converter diretamente
                retorno = acessos.ExecuteQuery($"SELECT dim_over_insul FROM {tableToSearch} WHERE size = '{cable.size}'");
                cable.d_i = Convert.ToDouble(retorno.Rows[0][0]) * 0.0254; // inch → m
            }
            else
            {
                // insul vem em mils → converter e somar duas vezes (acima e abaixo do condutor)
                retorno = acessos.ExecuteQuery($"SELECT insul FROM {tableToSearch} WHERE size = '{cable.size}'");
                double insul_m = Convert.ToDouble(retorno.Rows[0][0]) * 0.0000254; // mil → m
                cable.d_i = cable.d_c + 2 * insul_m;
            }



            // Sizing Proximity Effect

            cable.Y_prox = cable.Y_skin * Math.Pow(cable.d_c / cable.d_i, 2) * (0.312 * Math.Pow(cable.d_c / cable.d_i, 2) + (1.18 * cable.Y_skin) + 0.27);

            cable.Y_c = cable.Y_skin + cable.Y_prox;

            cable.Rac = cable.Rdc_TC_ft * (1 + cable.Y_c);

            cable.Rac_m = cable.Rac / 0.3048; 

            if (cable.voltage >= 2)
            {

                double V_ph = cable.voltage / Math.Sqrt(3); // tensão fase-terra em kV
                cable.Wd = 0.00276 * Math.Pow(V_ph, 2) * e_r * cosFi / Math.Log10(cable.d_i / cable.d_c);
            }
            else
            {
                cable.Wd = 0;
            }


            double rho_ins_m = 0.035; // °C·m/W = 350 / 10000
            cable.R_Ins = 0.012 * rho_ins_m * Math.Log10(cable.d_i / cable.d_c);

            // Removido *3, pois é per conductor
            cable.deltaTD = cable.Wd * (0.5 * cable.R_Ins + cable.R_ext);


            cable.R_air = 18.5 / (1 + 0.024 * (cable.TC + cable.soilTemp) / 2);

            cable.conduitSize = Convert.ToDouble(cmbConduits.Text);

            retorno = acessos.ExecuteQuery($"Select Average_OD_in, SCH40_Minimum_Wall from conduitsNeher where Size = '{cable.conduitSize}'");

            cable.D_i_duct = Convert.ToDouble(retorno.Rows[0][0]);

            cable.D_o_duct = cable.D_i_duct + 2 * Convert.ToDouble(retorno.Rows[0][1]);

            double rho_duct = 650; 

            cable.R_duct = 0.012 * rho_duct * Math.Log10(cable.D_o_duct / cable.D_i_duct);

            // Calculate R_earth

            cable.rho_soil = Convert.ToDouble(txtRhoSoil.Text) / 100;

            cable.H = Convert.ToDouble(txtH.Text);

            string[,] matriz = ObterValoresPreenchidosDoGrid();

            // Removido /2.54, e min_spacing ajustado para 1.5" (típico para dutos PVC próximos; altere para 0 se touching)
            cable.R_earth = CalculateREarth(matriz, cable.H, cable.rho_soil, 3);

            // Adicionar LF para R_earth (assumindo txtLoadFactor é um novo TextBox adicionado no XAML)
            double f = cable.LF;
            double LF = 0.3 * f + 0.7 * f * f;
            cable.R_earth *= LF;


            cable.R_ext = cable.R_air + cable.R_duct + cable.R_earth;

            cable.RCA = cable.R_Ins + cable.R_ext;


            cable.I = NeherFinalCurrent(cable);


            txttrash.Text = cable.I.ToString("F2");
            rich1.Document.Blocks.Clear();

            rich1.AppendText($"Rdc_TC em pés: {cable.Rdc_TC_ft}\n\n\n");
            rich1.AppendText($"Rdc_TC: {cable.Rdc_TC}\n");
            rich1.AppendText($"Rdc_25C: {cable.Rdc_25C}\n");
            rich1.AppendText($"Xs: {cable.Xs}\n");
            rich1.AppendText($"Y_skin: {cable.Y_skin}\n");
            rich1.AppendText($"TC: {cable.TC}\n");
            rich1.AppendText($"size: {cable.size}\n");
            rich1.AppendText($"d_c: {cable.d_c}\n");
            rich1.AppendText($"d_i: {cable.d_i}\n");
            rich1.AppendText($"voltage: {cable.voltage}\n");
            rich1.AppendText($"Y_prox: {cable.Y_prox}\n");
            rich1.AppendText($"Y_c: {cable.Y_c}\n");
            rich1.AppendText($"Rac: {cable.Rac}\n");
            rich1.AppendText($"Rac em metros: {cable.Rac_m}\n");
            rich1.AppendText($"Wd: {cable.Wd}\n");
            rich1.AppendText($"soilTemp: {cable.soilTemp}\n");
            rich1.AppendText($"R_air: {cable.R_air}\n");
            rich1.AppendText($"R_duct: {cable.R_duct}\n");
            rich1.AppendText($"D_o_duct: {cable.D_o_duct}\n");
            rich1.AppendText($"D_i_duct: {cable.D_i_duct}\n");
            rich1.AppendText($"conduitSize: {cable.conduitSize}\n");
            rich1.AppendText($"R_earth: {cable.R_earth}\n");
            rich1.AppendText($"rho_soil: {cable.rho_soil}\n");
            rich1.AppendText($"H: {cable.H}\n");
            rich1.AppendText($"R_ext: {cable.R_ext}\n");
            rich1.AppendText($"deltaTD: {cable.deltaTD}\n");
            rich1.AppendText($"R_Ins: {cable.R_Ins}\n");
            rich1.AppendText($"RCA: {cable.RCA}\n");
            rich1.AppendText($"I: {cable.I}\n");
            rich1.AppendText($"Y_sh: {cable.Y_sh}\n");
            rich1.AppendText($"R_sh: {cable.R_sh}\n");
            rich1.AppendText($"F_sh: {cable.F_sh}\n");

        }

        public static double NeherFinalCurrent(Cable cable)
        {
            double TC = cable.TC;
            double T_amb = cable.soilTemp;
            double R_ac = cable.Rac;
            double R_ins = cable.R_Ins;
            double R_ext = cable.R_ext;
            double Wd = cable.Wd;

            double I = 1;
            double step = 0.1;

            while (I < 2000)
            {
                double length_ft = 1000.0; // padrão NEC/ETAP

                double Pj = I * I * R_ac * length_ft;
                double deltaT = Pj * (R_ins + R_ext);
                double T_calc = T_amb + deltaT;

                if (T_calc > TC)
                    return Math.Round(I - step, 2);

                I += step;
            }

            return I;
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
                    if (InputGrid.Children[index] is Border border && border.Child is TextBox tb)
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

        private static Dictionary<double, double> d_o_lookup = new Dictionary<double, double>
    {
        {2, 2.375}, {3, 3.5}, {4, 4.5}, {5, 5.563}, {6, 6.625} // Adicione mais tamanhos se necessário
    };

        public static double CalculateREarth(string[,] matrix, double L, double rho_soil, double min_spacing = 3.0)
        {
            int num_rows = matrix.GetLength(0);
            int num_cols = matrix.GetLength(1);

            // Parse matrix for nominal sizes and target position
            List<List<double>> diams = new List<List<double>>();
            (int row, int col) target_pos = (-1, -1);

            for (int i = 0; i < num_rows; i++)
            {
                List<double> row_diams = new List<double>();
                for (int j = 0; j < num_cols; j++)
                {
                    string cell = matrix[i, j];
                    double d = double.Parse(cell.Replace("D", "").Trim());
                    row_diams.Add(d);
                    if (cell.Contains("D"))
                        target_pos = (i, j);
                }
                diams.Add(row_diams);
            }

            // Step 1: ajustar profundidade para centro do conduto alvo
            double depth_to_target_center = 0;
            for (int i = 0; i <= target_pos.row; i++)
            {
                double max_d_o_row = 0;
                foreach (double nom in diams[i])
                {
                    if (nom > 0 && d_o_lookup.ContainsKey(nom))
                        max_d_o_row = Math.Max(max_d_o_row, d_o_lookup[nom]);
                }

                if (i < target_pos.row)
                {
                    depth_to_target_center += max_d_o_row + min_spacing;
                }
                else
                {
                    depth_to_target_center += max_d_o_row / 2.0;
                }
            }

            double L_adjusted = L + depth_to_target_center;

            // Step 2: calcular y_rows (profundidade de cada linha a partir de L ajustado)
            List<double> y_rows = new List<double>();
            double current_bottom = L_adjusted;

            for (int i = 0; i < num_rows; i++)
            {
                double max_d_o_row = 0;
                foreach (double nom in diams[i])
                {
                    if (nom > 0 && d_o_lookup.ContainsKey(nom))
                        max_d_o_row = Math.Max(max_d_o_row, d_o_lookup[nom]);
                }

                double y_center = current_bottom + (max_d_o_row / 2);
                y_rows.Add(y_center);

                current_bottom = y_center + (max_d_o_row / 2);
                if (i < num_rows - 1)
                    current_bottom += min_spacing;
            }

            // Step 3: calcular x_cols para cada linha
            List<List<double>> x_rows = new List<List<double>>();
            for (int i = 0; i < num_rows; i++)
            {
                List<double> x_cols = new List<double>();
                double current_left = 0;

                for (int j = 0; j < num_cols; j++)
                {
                    double nom = diams[i][j];
                    if (nom <= 0) { x_cols.Add(0); continue; }

                    double d_o = d_o_lookup[nom];
                    double x_center;

                    if (j == 0 || x_cols.Count == 0)
                    {
                        x_center = d_o / 2;
                    }
                    else
                    {
                        double prev_nom = diams[i][j - 1];
                        double prev_d_o = d_o_lookup[prev_nom];
                        x_center = x_cols[j - 1] + (prev_d_o / 2) + (d_o / 2) + min_spacing;
                    }

                    x_cols.Add(x_center);
                }

                x_rows.Add(x_cols);
            }

            // Step 4: montar lista de dutos com posição e se é o alvo
            List<Dictionary<string, double>> ducts = new List<Dictionary<string, double>>();
            for (int i = 0; i < num_rows; i++)
            {
                for (int j = 0; j < num_cols; j++)
                {
                    double nom = diams[i][j];
                    if (nom <= 0) continue;

                    var duct = new Dictionary<string, double>
                    {
                        ["x"] = x_rows[i][j],
                        ["y"] = y_rows[i],
                        ["r"] = d_o_lookup[nom] / 2,
                        ["lf"] = 1.0, // fator de carga igual para todos
                        ["is_target"] = (i == target_pos.row && j == target_pos.col) ? 1 : 0
                    };

                    ducts.Add(duct);
                }
            }

            // Step 5: encontrar o conduto alvo
            var target = ducts.Find(d => d["is_target"] == 1);
            if (target == null)
                throw new Exception("Nenhum duto com 'D' encontrado.");

            // Step 6: somatório dos logaritmos
            double sum_log = 0.0;
            foreach (var m in ducts)
            {
                double d_image, d;

                if (m == target)
                {
                    d_image = 2 * target["y"];
                    d = target["r"];
                }
                else
                {
                    double dx = target["x"] - m["x"];
                    double dy = target["y"] - m["y"];
                    d = Math.Sqrt(dx * dx + dy * dy);
                    double dy_image = target["y"] + m["y"];
                    d_image = Math.Sqrt(dx * dx + dy_image * dy_image);
                }

                if (d == 0) d = target["r"];

                double log_term = Math.Log10(d_image / d);
                sum_log += log_term * m["lf"];
            }

            // Step 7: calcular R_earth final
            double r_earth = 0.012 * rho_soil * sum_log;

            return r_earth;
        }


        private void cmbTBase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        // load cables according voltage level
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            double voltageLevel = Convert.ToDouble(txtVoltageLevel.Text);
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

            cmbConduits.ItemsSource = conduits;
            cmbConduits.SelectedIndex = 10;

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in InputGrid.Children)
            {
                if (child is Border border && border.Child is TextBox tb)
                {
                    tb.Text = string.Empty;
                }
            }
        }



        /// <summary>
        ///  Bloco IEE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
    }
}