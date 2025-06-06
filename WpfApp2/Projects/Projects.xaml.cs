using System;
using System.Collections.Generic;
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


    }
}

