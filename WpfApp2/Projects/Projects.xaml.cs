using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
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
using ControlzEx.Standard;

namespace WpfApp2.Projects
{
    /// <summary>
    /// Interaction logic for Projects.xaml
    /// </summary>
    public partial class Projects : UserControl
    {
        DatabaseAccess acessos = new DatabaseAccess();
        private List<string> listProjects = new();
        private List<string> listPlants = new();
        private List<string> listNames = new();

        public Projects()
        {

            InitializeComponent();
            // Inicializar a instância do DatabaseAccess
            acessos = new DatabaseAccess();

            // Carregar os dados de forma assíncrona
            LoadProjectsAsync();

        }

        private async void LoadProjectsAsync()
        {
            try
            {
                // Mostrar a barra de progresso e ocultar o conteúdo principal
                LoadingIndicator.Visibility = Visibility.Visible;


                // Executar a consulta em uma thread separada para não bloquear a UI
                DataTable projectsData = await Task.Run(() =>
                {
                    return acessos.ExecuteQuery("SELECT Code, Name FROM projects");
                });

                gridProjetos.ItemsSource = projectsData.DefaultView;


                DataTable plantsData = await Task.Run(() =>
                {
                    return acessos.ExecuteQuery("SELECT Plant FROM plants");
                });

                gridPlants.ItemsSource = plantsData.DefaultView;

                // Verificar se obtivemos dados
                if (projectsData != null && projectsData.Rows.Count > 0)
                {
                    // Preencher o DataGrid com os dados dos projetos
                    listProjects = projectsData.AsEnumerable()
                                                                .Select(row => row["Code"].ToString())
                                                                .ToList();

                    cmbProject.ItemsSource = listProjects;

                    listNames = projectsData.AsEnumerable()
                                                                .Select(row => row["Name"].ToString())
                                                                .ToList();

                    cmbName.ItemsSource = listNames;

                    listPlants = plantsData.AsEnumerable()
                                                                .Select(row => row["Plant"].ToString())
                                                                .ToList();
                    cmbPlant.ItemsSource = listPlants;



                }
                else
                {
                    // Se não há dados, mostrar uma mensagem
                    MessageBox.Show("Nenhum projeto encontrado na base de dados.",
                                    "Informação",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }


            }
            catch (Exception ex)
            {
                // Tratar erros e mostrar mensagem ao usuário
                MessageBox.Show($"Erro ao carregar projetos: {ex.Message}",
                                "Erro",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                // Ocultar a barra de progresso e mostrar o conteúdo principal
                LoadingIndicator.Visibility = Visibility.Collapsed;

            }
        }


        private void cmbProject_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            string textoDigitado = cmbProject.Text;

            var itensFiltrados = listProjects
                .Where(item => item.IndexOf(textoDigitado, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            cmbProject.ItemsSource = itensFiltrados;

            cmbProject.IsDropDownOpen = true;
        }

        private void cmbPlant_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            string textoDigitado = cmbPlant.Text;

            var itensFiltrados = listPlants
                .Where(item => item.IndexOf(textoDigitado, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            cmbPlant.ItemsSource = itensFiltrados;

            cmbPlant.IsDropDownOpen = true;


        }

        private void cmbName_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            string textoDigitado = cmbName.Text;

            var itensFiltrados = listNames
                .Where(item => item.IndexOf(textoDigitado, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            cmbName.ItemsSource = itensFiltrados;
            cmbName.IsDropDownOpen = true;
        }

        private async void ProjectsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridProjetos.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)gridProjetos.SelectedItem;

                txtSelectedProject.Text = selectedRow["Code"].ToString();

                LoadingIndicator.Visibility = Visibility.Visible;

                DataTable plantsData = await Task.Run(() =>
                {
                    return acessos.ExecuteQuery($"SELECT Plant FROM plants where Code ='{selectedRow["Code"].ToString()}'");
                });


                gridPlants.ItemsSource = plantsData.DefaultView;

                LoadingIndicator.Visibility = Visibility.Collapsed;



            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string varSql = "iNSERT into projects (Code, Name) values ('" + cmbProject.Text + "','" + cmbName.Text + "')";
            int rowsAffected = acessos.ExecuteNonQuery(varSql);

            if (rowsAffected > 0)
            {
                MessageBox.Show("Project Added", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadProjectsAsync(); // Recarregar os projetos para atualizar a lista
            }
            else
            {
                MessageBox.Show("Error.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtSelectedProject.Text = cmbProject.Text;




        }

        private void Save_PlanT(object sender, RoutedEventArgs e)
        {
            string varSql = "Insert into plants (Plant, Code) values ('" + cmbPlant.Text + "','" + txtSelectedProject.Text + "')";
            int rowsAffected = acessos.ExecuteNonQuery(varSql);
            if (rowsAffected > 0)
            {
                MessageBox.Show("Plant Added", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadProjectsAsync(); // Recarregar os projetos para atualizar a lista
            }
            else
            {
                MessageBox.Show("Error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {


        }



        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    // Verifica se o cabeçalho da aba ativa é "Feeders"
                    if (selectedTab.Header?.ToString() == "Feeders")
                    {
                        string selectedProject = txtSelectedProject.Text;

                        string varSql = "select distinct plant from plants where code = '" + selectedProject + "'";


                        DataTable projectsData = await Task.Run(() =>
                        {
                            return acessos.ExecuteQuery(varSql);
                        });

                        List<string> plants = projectsData.AsEnumerable()
                            .Select(row => row.Field<string>("Plant"))
                            .ToList();

                        plants.Add("GENERAL");

                        cmbProjectsFeeder.ItemsSource = plants;
                        cmbFromPlantTrafo.ItemsSource = plants;
                        cmbToPlantTrafo.ItemsSource = plants;

                        gridFeeders.ItemsSource = acessos.ExecuteQuery("Select id, plant, level, tag, descr from feeders where project = '" + selectedProject + "'").DefaultView;

                        gridTrafos.ItemsSource = acessos.ExecuteQuery("select id, high, low, tag, power, fromPlant, toPlant from trafos where project = '" + selectedProject + "'").DefaultView;

                    }



                    if (selectedTab.Header?.ToString() == "Loads")
                    {
                        string selectedProject = txtSelectedProject.Text;

                        string varSql = "select distinct plant from plants where code = '" + selectedProject + "'";


                        DataTable projectsData = await Task.Run(() =>
                        {
                            return acessos.ExecuteQuery(varSql);
                        });

                        List<string> plants = projectsData.AsEnumerable()
                            .Select(row => row.Field<string>("Plant"))
                            .ToList();

                        plants.Add("GENERAL");

                        cmbPlantsLoad.ItemsSource = plants;

                        List<string> auxiliarUnidades = new();
                        auxiliarUnidades.Add("HP");
                        auxiliarUnidades.Add("kW");
                        auxiliarUnidades.Add("kVA");
                        cmbPowerUnitsMV.ItemsSource = auxiliarUnidades;

                        List<string> loadTypes = new();
                        loadTypes.Add("Motor");
                        loadTypes.Add("Generator");
                        loadTypes.Add("Heater");
                        loadTypes.Add("Feeder");
                        cmbLVLoadType.ItemsSource = loadTypes;

                        varSql = $@"
                                SELECT id,
                                fromPanel,
                                fromUnit,
                                power,
                                powerUnit,
                                tag,
                                descr,
                                'MV' AS voltageLevel
                            FROM
                                mvLoads
                            WHERE
                                project = '{txtSelectedProject.Text}' AND
                                plant = '{cmbPlantsLoad.Text}'

                            UNION ALL

                            SELECT id,
                                fromPanel,
                                fromUnit,
                                power,
                                powerUnit,
                                tag,
                                descr,
                                'LV' AS voltageLevel
                            FROM
                                lvLoads
                            WHERE
                                project = '{txtSelectedProject.Text}' AND
                                plant = '{cmbPlantsLoad.Text}';
                            ";

                        gridLoads.ItemsSource = acessos.ExecuteQuery(varSql).DefaultView;

                    }

                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        // load existent panels
        private void cmbProjectsFeeder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            string varSql = $"Select tag from feeders where type = 'Panel' and project = '{txtSelectedProject.Text}' and plant = '{cmbProjectsFeeder.SelectedValue.ToString()}'";

            DataTable retorno = acessos.ExecuteQuery(varSql);
            List<string> itens = retorno.AsEnumerable()
                           .Select(row => row.Field<string>("tag"))
                           .ToList();

            listPanels.ItemsSource = null; // Clear the previous items

            listPanels.ItemsSource = itens;
        }

        private void btnAddPanels_Click(object sender, RoutedEventArgs e)
        {
            string selectedProject = txtSelectedProject.Text;
            string selectedPlant = cmbProjectsFeeder.Text;
            string voltageLevel = txtPanelLevel.Text + cmbUnitLevel.Text;
            string panelName = txtTagPanel.Text;
            string description = txtDescPanel.Text;

            string varSql = $"INSERT INTO feeders (tag, type, project, plant, level, descr, dateReg, location) " +
                            $"VALUES ('{panelName}', 'Panel', '{selectedProject}', '{selectedPlant}', '{voltageLevel}', '{description}', '{DateTime.Now.ToString()}', '{txtPanelLocation.Text}')";

            int rowsAffected = acessos.ExecuteNonQuery(varSql);
            if (rowsAffected != 0)
            {
                if (rowsAffected == 1)
                {
                    MessageBox.Show("Panel Added", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    cmbProjectsFeeder_SelectionChanged(sender, null);

                }
                else
                {
                    MessageBox.Show("Error adding panel.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);


                }
            }
            else
            {
                MessageBox.Show("Error executing query.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        bool controleFromPlantTrafo = false;
        private async void cmbFromPlantTrafo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!controleFromPlantTrafo)
            {
                string selectedPlant = cmbFromPlantTrafo.SelectedValue?.ToString();

                string varSql = "Select tag from feeders where type = 'Panel' and plant = '" + selectedPlant + "'";

                DataTable retorno = await Task.Run(() =>
                {
                    return acessos.ExecuteQuery(varSql);
                });


                if (retorno != null)
                {
                    List<string> itens = retorno.AsEnumerable()
                                   .Select(row => row.Field<string>("tag"))
                                   .ToList();
                    cmbFromPanelTrafo.Items.Clear(); // Clear the previous items
                    cmbFromPanelTrafo.ItemsSource = itens;
                }
                else
                {
                    MessageBox.Show("Error retrieving panels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private void btnAddPanels_Copy_Click(object sender, RoutedEventArgs e)


        {



            string varSql = $"INSERT INTO trafos (high, low, tag, power, fromPlant, fromPanel, project, dateAdded, toPlant, toPanel, fromUnit, toUnit) VALUES (" +
       $"'{txtTrafHV.Text}{cmbLevelUnitHV.SelectedValue}', " +
       $"'{txtTrafLV.Text}{cmbLevelUnitLV.SelectedValue}', " +
       $"'{txtTagTrafo.Text}', " +
       $"'{txtPowerTrafo.Text}{cmbLevelPotTrafo.SelectedValue}', " +
       $"'{cmbFromPlantTrafo.Text}', " +
       $"'{cmbFromPanelTrafo.Text}', " +
       $"'{txtSelectedProject.Text}', " +
       $"'{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '{cmbToPlantTrafo.Text}', '{cmbToPanelTrafo.Text}', '{txtFromUnitTrafo.Text}', '{txtToUnitTrafo.Text}')";  // <-- Formato de data seguro

            int rowsAffected = acessos.ExecuteNonQuery(varSql);
            if (rowsAffected != 0)
            {
                if (rowsAffected == 1)
                {
                    MessageBox.Show("Transformer Added", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Error adding transformer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Error executing query.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        bool controleToPlantTrafo = false;

        private async void cmbToPlantTrafo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!controleFromPlantTrafo)
            {
                string selectedPlant = cmbToPlantTrafo.SelectedValue?.ToString();

                string varSql = "Select tag from feeders where type = 'Panel' and plant = '" + selectedPlant + "'";

                DataTable retorno = await Task.Run(() =>
                {
                    return acessos.ExecuteQuery(varSql);
                });


                if (retorno != null)
                {
                    List<string> itens = retorno.AsEnumerable()
                                   .Select(row => row.Field<string>("tag"))
                                   .ToList();
                    cmbToPanelTrafo.Items.Clear(); // Clear the previous items
                    cmbToPanelTrafo.ItemsSource = itens;
                }
                else
                {
                    MessageBox.Show("Error retrieving panels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void cmbPlantsLoad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            string varSql = $"Select distinct tag from feeders where project = '{txtSelectedProject.Text}' and plant = '" + cmbPlantsLoad.SelectedValue.ToString() + "'";
            DataTable retorno = acessos.ExecuteQuery(varSql);
            if (retorno != null)
            {
                List<string> itens = retorno.AsEnumerable()
                               .Select(row => row.Field<string>("tag"))
                               .ToList();
                cmbMVFromPanel.ItemsSource = itens;
                cmbLVFromPanel.ItemsSource = itens;

                recallLoadsGrid();



            }
            else
            {
                MessageBox.Show("Error retrieving panels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddMVLoad_Click(object sender, RoutedEventArgs e)
        {
            string sql = $@"
INSERT INTO mvLoads 
(project, plant, fromPanel, fromUnit, power, powerUnit, descr, dateRegister, 
 selPower, selFiber, ctWiring, exciter, speed, terminal, motorHeater, tag)
VALUES (
    '{txtSelectedProject.Text}',
    '{cmbPlantsLoad.SelectedValue?.ToString()}',
    '{cmbMVFromPanel.SelectedValue?.ToString()}',
    '{txtMVUnit.Text}',
    '{txtPowerMV.Text}',
    '{cmbPowerUnitsMV.SelectedValue?.ToString()}',
    '{txtDescrMV.Text}',
    '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}',
    {(chkSELPower.IsChecked == true ? 1 : 0)},
    {(chkSELFiberCable.IsChecked == true ? 1 : 0)},
    {(chkCTWiring.IsChecked == true ? 1 : 0)},
    {(chkExciterControlPower.IsChecked == true ? 1 : 0)},
    {(chkSpeedSwitchWiring.IsChecked == true ? 1 : 0)},
    {(chkTerminalBoxHeaterWiring.IsChecked == true ? 1 : 0)}, {(chkMotorSpaceHeater.IsChecked == true ? 1 : 0)}, '{txtTagMV.Text}'
)";

            int rowsAffected = acessos.ExecuteNonQuery(sql);
            if (rowsAffected > 0)
            {
                MySnackbar.MessageQueue?.Enqueue("Added Successfully");
                recallLoadsGrid();
            }
            else
            {
                MessageBox.Show("Error adding load.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbLVLoadType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> unidadesMotor = new();
            unidadesMotor.Add("HP");
            unidadesMotor.Add("kW");

            List<string> unidadesGerador = new();
            unidadesGerador.Add("KVA");

            List<string> unidadeResistor = new();
            unidadeResistor.Add("kW");
            List<string> unidadeFeeder = new();
            unidadeFeeder.Add("A");


            if (cmbLVLoadType.SelectedItem == "Motor")
            {
                chkVFD.Visibility = Visibility.Visible;
                lblPowerLV.Text = "Power";
                cmbLVUnidade.ItemsSource = unidadesMotor;
                cmbLVUnidade.SelectedIndex = 0;
            }
            else if (cmbLVLoadType.SelectedItem == "Generator")
            {
                chkVFD.Visibility = Visibility.Collapsed;
                lblPowerLV.Text = "Power";
                cmbLVUnidade.ItemsSource = unidadesGerador;
                cmbLVUnidade.SelectedIndex = 0;

            }
            else if (cmbLVLoadType.SelectedItem == "Heater")
            {
                chkVFD.Visibility = Visibility.Collapsed;
                lblPowerLV.Text = "Power";
                cmbLVUnidade.ItemsSource = unidadeResistor;
                cmbLVUnidade.SelectedIndex = 0;
            }
            else if (cmbLVLoadType.SelectedItem == "Feeder")
            {
                chkVFD.Visibility = Visibility.Collapsed;
                lblPowerLV.Text = "Circ. Br.";
                cmbLVUnidade.ItemsSource = unidadeFeeder;
                cmbLVUnidade.SelectedIndex = 0;


            }

        }

        private void btnLVAdd_Click(object sender, RoutedEventArgs e)
        {

            int vfdCheck = chkVFD.IsChecked == true ? 1 : 0;

            string sql = $@"
INSERT INTO lvLoads 
(project, plant, fromPanel, fromUnit, loadType, power, powerUnit, tag, descr, dateRegister, isVFD)
VALUES (
    '{txtSelectedProject.Text}',
    '{cmbPlantsLoad.SelectedValue?.ToString()}',
    '{cmbLVFromPanel.SelectedValue?.ToString()}',
    '{txtLVUnit.Text}',
    '{cmbLVLoadType.SelectedValue?.ToString()}',
    '{txtPowerLV.Text}',
    '{cmbLVUnidade.SelectedValue?.ToString()}',
    '{txtLVTag.Text}',
    '{txtLVDesc.Text}',
    '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}', '{vfdCheck}'
)";

            int rowsAffected = acessos.ExecuteNonQuery(sql);
            if (rowsAffected > 0)
            {
                MySnackbar.MessageQueue?.Enqueue("Added Successfully");
                recallLoadsGrid();
            }
            else
            {
                MessageBox.Show("Error adding load.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string elemento = txtLVUnit.Text;
            int? inteiro;
            try
            {
                inteiro = Convert.ToInt16(elemento);
                inteiro++;
                txtLVUnit.Text = inteiro.ToString();
                MySnackbar.MessageQueue?.Enqueue("Plus Unit");
            }
            catch
            {

            }
        }

        public void recallLoadsGrid()
        {

            string varSql = $@"
                                SELECT id,
                                fromPanel,
                                fromUnit, loadType,
                                power,
                                powerUnit,
                                tag,
                                descr
                            FROM
                                lvLoads
                            WHERE
                                project = '{txtSelectedProject.Text}' AND
                                plant = '{cmbPlantsLoad.SelectedValue}'";

            DataTable respostaLV = acessos.ExecuteQuery(varSql);

            varSql = $"select fromPanel, fromUnit, power, powerUnit, tag, descr from mvLoads where project = '{txtSelectedProject.Text}' and plant = '{cmbPlantsLoad.SelectedValue}'";

            DataTable respostaMV = acessos.ExecuteQuery(varSql);

            // Clona a estrutura da tabela respostaLV
            DataTable tabelaUnificada = respostaLV.Clone();

            // Adiciona a coluna "Level" à tabela unificada
            tabelaUnificada.Columns.Add("Level", typeof(string));

            // Adiciona colunas que estão em respostaMV mas não em respostaLV
            foreach (DataColumn col in respostaMV.Columns)
            {
                if (!tabelaUnificada.Columns.Contains(col.ColumnName))
                {
                    tabelaUnificada.Columns.Add(col.ColumnName, col.DataType);
                }
            }

            // Copia os dados da respostaLV
            foreach (DataRow row in respostaLV.Rows)
            {
                DataRow newRow = tabelaUnificada.NewRow();
                foreach (DataColumn col in respostaLV.Columns)
                {
                    newRow[col.ColumnName] = row[col.ColumnName];
                }
                newRow["Level"] = "LV";
                tabelaUnificada.Rows.Add(newRow);
            }

            // Copia os dados da respostaMV
            foreach (DataRow row in respostaMV.Rows)
            {
                DataRow newRow = tabelaUnificada.NewRow();
                foreach (DataColumn col in respostaMV.Columns)
                {
                    newRow[col.ColumnName] = row[col.ColumnName];
                }
                newRow["Level"] = "MV";
                tabelaUnificada.Rows.Add(newRow);
            }



            gridLoads.ItemsSource = tabelaUnificada.DefaultView;
        }

        private void gridTrafos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //não permite que haja alteração dos paineis quando mudar as plantas

            controleFromPlantTrafo = true;
            controleToPlantTrafo = true;


            int selectedId = 0;
            if (gridTrafos.SelectedItem != null)
            {
                try
                {
                    // Cast para DataRowView
                    DataRowView selectedRow = (DataRowView)gridTrafos.SelectedItem;

                    // Acessa a coluna pelo nome
                    selectedId = Convert.ToInt32(selectedRow["id"]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao obter ID: {ex.Message}");
                }
            }



            string varSql = "select * from trafos where id = " + selectedId;

            DataTable trafoData = acessos.ExecuteQuery(varSql);

            trafo selectedTrafo = new();

            selectedTrafo.Id = Convert.ToInt16(trafoData.Rows[0]["id"]);
            selectedTrafo.High = trafoData.Rows[0]["high"].ToString();
            selectedTrafo.Low = trafoData.Rows[0]["low"].ToString();
            selectedTrafo.Tag = trafoData.Rows[0]["tag"].ToString();
            selectedTrafo.Power = trafoData.Rows[0]["power"].ToString();
            selectedTrafo.FromPlant = trafoData.Rows[0]["fromPlant"].ToString();
            selectedTrafo.FromPanel = trafoData.Rows[0]["fromPanel"].ToString();
            selectedTrafo.Project = trafoData.Rows[0]["project"].ToString();
            selectedTrafo.DateAdded = trafoData.Rows[0]["dateAdded"].ToString();
            selectedTrafo.ToPlant = trafoData.Rows[0]["toPlant"].ToString();
            selectedTrafo.ToPanel = trafoData.Rows[0]["toPanel"].ToString();
            selectedTrafo.FromUnit = trafoData.Rows[0]["fromUnit"].ToString();
            selectedTrafo.ToUnit = trafoData.Rows[0]["toUnit"].ToString();

            var highSplited = calc.Separar(selectedTrafo.High);
            var lowSplited = calc.Separar(selectedTrafo.Low);

            txtTrafHV.Text = highSplited.Item1.ToString();
            cmbLevelUnitHV.SelectedValue = highSplited.Item2;
            txtTrafLV.Text = lowSplited.Item1.ToString();
            cmbLevelUnitLV.SelectedValue = lowSplited.Item2;
            cmbLevelUnitLV.SelectedIndex = 0;
            txtTagTrafo.Text = selectedTrafo.Tag;

            var powerTrafo = calc.Separar(selectedTrafo.Power);

            txtPowerTrafo.Text = powerTrafo.Item1.ToString();

            cmbLevelPotTrafo.SelectedValue = powerTrafo.Item2;
            cmbFromPlantTrafo.SelectedValue = selectedTrafo.FromPlant;
            cmbToPlantTrafo.SelectedValue = selectedTrafo.ToPlant;
            txtFromUnitTrafo.Text = selectedTrafo.FromUnit;
            txtToUnitTrafo.Text = selectedTrafo.ToUnit;


            cmbFromPanelTrafo.Items.Clear();
            cmbFromPanelTrafo.Items.Add(selectedTrafo.FromPanel);
            cmbFromPanelTrafo.SelectedIndex = 0;

            cmbToPanelTrafo.Items.Clear();
            cmbToPanelTrafo.Items.Add(selectedTrafo.ToPanel);
            cmbToPanelTrafo.SelectedIndex = 0;


            controleFromPlantTrafo = false;
            controleToPlantTrafo = false;

        }
        public int idSelecionado = 0;
        private void gridMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
             if (gridLoads.SelectedItem is DataRowView rowView)
            {
                if (int.TryParse(rowView["id"].ToString(), out idSelecionado))
                {
                    // Agora você tem o valor do ID na variável

                    string varSql = $"Select * from lvloads where id = '{idSelecionado}'";

                    DataTable retorno = acessos.ExecuteQuery(varSql);


                    DataRow row = retorno.Rows[0];
                    cmbLVFromPanel.Text = row["fromPanel"].ToString();
                    txtLVUnit.Text = row["fromUnit"].ToString();
                    cmbLVLoadType.Text = row["loadType"].ToString();
                    txtPowerLV.Text = row["power"].ToString();
                    cmbLVUnidade.Text = row["powerUnit"].ToString();
                    txtLVTag.Text = row["tag"].ToString();
                    txtLVDesc.Text = row["descr"].ToString();
                    chkVFD.IsChecked = row["isVFD"].ToString() == "1" ? true: false; 

                    



                }
            }
        }
    }
}
