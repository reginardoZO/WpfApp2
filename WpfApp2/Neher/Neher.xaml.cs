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

        private void CriarGradeNumerica(int rows, int columns)
        {
            InputGrid.Rows = rows;
            InputGrid.Columns = columns;

            for (int i = 0; i < rows * columns; i++)
            {
                var textBox = new System.Windows.Controls.TextBox
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

            double menorCorrente = double.MaxValue;


            string[,] matriz = ObterValoresPreenchidosDoGrid();



            List<(int row, int col, double size)> conduitsToCalculate = new();

            for (int i = 0; i < matriz.GetLength(0); i++)
            {
                for (int j = 0; j < matriz.GetLength(1); j++)
                {
                    if (!string.IsNullOrWhiteSpace(matriz[i, j]))
                    {
                        string clean = new string(matriz[i, j].Where(c => char.IsDigit(c) || c == '.').ToArray());
                        if (double.TryParse(clean, out double val))
                        {
                            conduitsToCalculate.Add((i, j, val));
                        }
                    }
                }
            }



            StringBuilder resultados = new();

            foreach (var (row, col, size) in conduitsToCalculate)
            {
                Cable cable = new();

                cable.conduitSize = size;

                // Parâmetros comuns (apenas os fixos para todos)
                cable.LF = Convert.ToDouble(txtLF.Text) / 100;
                cable.voltage = Convert.ToDouble(txtVoltageMain.Text) / 1000;
                cable.soilTemp = Convert.ToDouble(txtSoilTemp.Text);
                cable.size = cmbCables.SelectedValue.ToString();
                cable.alfaDoCobre = 0.00393;
                cable.TC = Convert.ToDouble(cmbTemp.Text);
                cable.RhoDuctCm = Convert.ToDouble(txtRhoDuct.Text);
                cable.rho_soil = Convert.ToDouble(txtRhoSoil.Text);
                cable.H = Convert.ToDouble(txtH.Text) / 12.0;
                cable.RhoInsCm = 350.0;

                double corrente = CalcularAmpacidade(cable, matriz, row, col);

                if (corrente < menorCorrente)
                    menorCorrente = corrente;

                resultados.AppendLine($"{row + 1},{col + 1} - {corrente:F2} A");
            }

            // Limpa o conteúdo anterior
            richResults.Document.Blocks.Clear();

            // Cria novo parágrafo com os resultados
            Paragraph paragraph = new Paragraph(new Run(resultados.ToString()));

            // Adiciona ao RichTextBox
            richResults.Document.Blocks.Add(paragraph);

            txttrash.Text = menorCorrente.ToString("F2");


        }

        private double CalcularAmpacidade(Cable cable, string[,] matriz, int row, int col)
        {

                       

            double e_r = 0;

            double cosFi = 0;

            string tableToSearch = "";

            string varSql = "";

            if (cable.voltage >= 2)
            {
                tableToSearch = "medium_voltage";
                e_r = 3;
                cosFi = 0.002;
                varSql = $"SELECT rdc_25, rac_90 FROM {tableToSearch} WHERE size = '{cable.size}'";

            }
            else
            {
                e_r = 4;
                cosFi = 0.05;
                tableToSearch = "low_voltage";
                varSql = $"SELECT rdc_25, rac_75 FROM {tableToSearch} WHERE size = '{cable.size}'";
            }



            cable.TC = Convert.ToDouble(cmbTemp.Text);

            DataTable retorno = acessos.ExecuteQuery(varSql);

            cable.Rdc_25C = Convert.ToDouble(retorno.Rows[0][0]); // Ω/1000 ft


            if (cable.voltage >= 2)
            {
                cable.Rac_90 = Convert.ToDouble(retorno.Rows[0][1]); // Ω/1000 ft
                cable.Rdc_90 = cable.Rdc_25C * (1 + cable.alfaDoCobre * (90 - 25));
                cable.Y_skin = (cable.Rac_90 - cable.Rdc_90) / cable.Rdc_90; // Y_skin = (Rac - Rdc) / Rdc

            }
            else
            {
                cable.Rac_75 = Convert.ToDouble(retorno.Rows[0][1]); // Ω/1000 ft
                cable.Rdc_75 = cable.Rdc_25C * (1 + cable.alfaDoCobre * (75 - 25));
                cable.Y_skin = (cable.Rac_75 - cable.Rdc_75) / cable.Rdc_75;

            }

            retorno = acessos.ExecuteQuery($"SELECT dim_bare FROM {tableToSearch} WHERE size = '{cable.size}'");

            cable.d_c = Convert.ToDouble(retorno.Rows[0][0]);


            if (cable.voltage >= 2)
            {
                // dim_over_insul já é o diâmetro total em polegadas → converter diretamente
                retorno = acessos.ExecuteQuery($"SELECT dim_over_insul FROM {tableToSearch} WHERE size = '{cable.size}'");
                cable.s = Convert.ToDouble(retorno.Rows[0][0]); // inches
            }
            else
            {
                // insul vem em mils → converter e somar duas vezes (acima e abaixo do condutor)
                retorno = acessos.ExecuteQuery($"SELECT insul FROM {tableToSearch} WHERE size = '{cable.size}'");
                double insul = Convert.ToDouble(retorno.Rows[0][0]) / 1000; // inches
                cable.s = cable.d_c + 2 * insul;
            }

            // Sizing Proximity Effect

            cable.Y_prox = cable.Y_skin * Math.Pow(cable.d_c / cable.s, 2) * (0.312 * Math.Pow(cable.d_c / cable.s, 2) + (1.18 * cable.Y_skin) + 0.27);

            cable.Y_c = cable.Y_skin + cable.Y_prox;

            if (cable.voltage >= 2)
            {
                cable.Rac = cable.Rdc_90 * (1 + cable.Y_c);

                double V_ph = cable.voltage / Math.Sqrt(3); // tensão fase-terra em kV
                cable.Wd = 0.00276 * Math.Pow(V_ph, 2) * e_r * cosFi / Math.Log10(cable.s / cable.d_c);
            }
            else
            {
                cable.Rac = cable.Rdc_75 * (1 + cable.Y_c);
                cable.Wd = 0;
            }



            // Caclulate R_ins

            double rho_ins = cable.RhoInsCm; // Use o novo input; adicione txtRhoIns no XAML se necessário.
            cable.R_Ins = 0.012 * rho_ins * Math.Log10(cable.s / cable.d_c);

            //cable.R_Ins = 0.012 * rho_ins * Math.Log(cable.s / cable.d_c);

            // Calculate R_air

            cable.R_air = 18.5 / (1 + 0.024 * (cable.TC + cable.soilTemp) / 2);



            // Calculate R_duct





            retorno = acessos.ExecuteQuery($"Select Average_OD_in, SCH40_Minimum_Wall from conduitsNeher where Size = '{cable.conduitSize}'");

            cable.D_i_duct = Convert.ToDouble(retorno.Rows[0][0]);

            cable.D_o_duct = cable.D_i_duct + 2 * Convert.ToDouble(retorno.Rows[0][1]);



            // Resultado final em °C·in/W

            cable.RhoDuctCm = Convert.ToDouble(txtRhoDuct.Text); // Armazene na propriedade.
            double rho_duct_ft = cable.RhoDuctCm / 30.48; // Converta para °C·ft/W.
            cable.R_duct = (rho_duct_ft / (2 * Math.PI)) * Math.Log(cable.D_o_duct / cable.D_i_duct);




            // Calculate R_earth

            // rho_soil digitado em ºC·cm/W → converter para ºC·ft/W

            cable.rho_soil = Convert.ToDouble(txtRhoSoil.Text);

            // profundidade H em inches → converter para feet
            cable.H = Convert.ToDouble(txtH.Text) / 12.0;




            // Removido /2.54, e min_spacing ajustado para 1.5" (típico para dutos PVC próximos; altere para 0 se touching)
            cable.R_earth = CalculateREarth(matriz, cable.H, cable.rho_soil, 3, row, col);

            cable.R_ext = cable.R_air + cable.R_duct + cable.R_earth;

            // ------------- iteração para correção da temperatura do cabo ----------------

            // Inicializar variáveis de iteração
            double T_C = cable.TC; // temperatura alvo do núcleo do condutor
            cable.T_surface = T_C;  // chute inicial

            int iter = 0;
            int maxIter = 100;
            double tol = 0.1; // tolerância em °C

            do
            {
                double T_s_old = cable.T_surface;

                // Atualizar theta_m com base em T_surface e T_d
                cable.theta_m = (cable.T_surface + cable.soilTemp) / 2.0;

                // Atualizar R_air com base em theta_m
                cable.R_air = 18.5 / (1 + 0.024 * cable.theta_m);

                cable.R_air *= (1 + 0.2 * (3 - 1));

                // Atualizar R_ext com novo R_air
                cable.R_ext = cable.R_air + cable.R_duct + cable.R_earth;

                // Atualizar Rac conforme tipo de cabo
                if (cable.voltage >= 2)
                {
                    cable.Rac = cable.Rdc_90 * (1 + cable.Y_c);
                }
                else
                {
                    cable.Rac = cable.Rdc_75 * (1 + cable.Y_c);
                }

                // Corrente de carga (entrada do usuário)
                double I = Convert.ToDouble(txtCurrentPretend.Text); // A

                // Potência dissipada por unidade de comprimento (W/1000ft)
                double Q = Math.Pow(I, 2) * cable.Rac;

                // Converter para W/ft
                Q /= 1000.0;

                // Atualizar delta T no isolamento
                cable.deltaTD = Q * cable.R_Ins;

                // Nova temperatura de superfície
                cable.T_surface = T_C - cable.deltaTD;

                iter++;
            } while (Math.Abs(cable.T_surface - T_C + cable.deltaTD) > tol && iter < maxIter);


            cable.deltaTD = cable.Wd * (0.5 * cable.R_Ins + cable.R_ext);

            cable.RCA = cable.R_Ins + cable.R_ext;

            cable.I = NeherFinalCurrent(cable);
            return cable.I;
        }

        public static double NeherFinalCurrent(Cable cable)
        {
            double TC = cable.TC;
            double T_amb = cable.soilTemp;
            double R_Ins = cable.R_Ins;
            double R_ext = cable.R_ext;
            double Wd = cable.Wd;
            double Rdc_25C = cable.Rdc_25C;
            double alfa = cable.alfaDoCobre;
            double Yc = cable.Y_c;

            double I = 1.0;
            double step = 5;

            int iter = 0;
            string logPath = @"C:\temp\log.txt";

            // Criar a pasta se não existir
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));

            // Iniciar log
            File.WriteAllText(logPath, $"Log Neher-McGrath - {DateTime.Now}\n\n");

            while (I < 2000)
            {

                double Pj = I * I * (cable.Rac / 1000.0); // R_ac em ohm/1000ft

                double T_surface = T_amb + Pj * R_Ins;
                double theta_m = (T_surface + T_amb) / 2;

                double Rdc_theta = Rdc_25C * (1 + alfa * (theta_m - 25));
                double Rac = Rdc_theta * (1 + Yc);

                double deltaT = Pj * (R_Ins + R_ext) + Wd * (0.5 * R_Ins + R_ext);
                double T_calc = T_amb + deltaT;

                // Registrar a cada 50 iterações ou na última
                if (iter % 50 == 0 || T_calc > TC)
                {
                    string log = $"I = {I:F2} A\n" +
                                 $"  θ_m = {theta_m:F2} °C\n" +
                                 $"  Rdc_θ = {Rdc_theta:F6} Ω\n" +
                                 $"  Rac = {Rac:F6} Ω\n" +
                                 $"  deltaT = {deltaT:F2} °C\n" +
                                 $"  T_calc = {T_calc:F2} °C\n" +
                                 $"---------------------------\n";

                    File.AppendAllText(logPath, log);
                }

                if (T_calc >= TC)
                    return Math.Round(I - step, 2);

                I += step;
                iter++;
            }

            return Math.Round(I, 2);
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

        private static Dictionary<double, double> d_o_lookup = new Dictionary<double, double>
    {
        {2, 2.375}, {3, 3.5}, {4, 4.5}, {5, 5.563}, {6, 6.625} // Adicione mais tamanhos se necessário
    };

        public static double CalculateREarth(string[,] matrix, double L, double rho_soil, double min_spacing, int rowTarget, int colTarget)
        {
            int num_rows = matrix.GetLength(0);
            int num_cols = matrix.GetLength(1);

            // Parse matrix for nominal sizes and target position
            List<List<double>> diams = new List<List<double>>();
            (int row, int col) target_pos = (rowTarget, colTarget);

            for (int i = 0; i < num_rows; i++)
            {
                List<double> row_diams = new List<double>();
                for (int j = 0; j < num_cols; j++)
                {
                    string cell = matrix[i, j];
                    double d = double.Parse(cell.Replace("D", "").Trim());
                    row_diams.Add(d);
                    //if (cell.Contains("D"))
                    //    target_pos = (i, j);
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

            double L_inches = L * 12;

            double current_bottom = L_inches;

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
    }
}