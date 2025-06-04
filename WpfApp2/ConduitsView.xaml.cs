using System.Data;
using System.Diagnostics;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Windows.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for ConduitsView.xaml
    /// </summary>
    public partial class ConduitsView : UserControl
    {
        public DatabaseAccess acessos = new DatabaseAccess();
        DataTable databaseLoad = new DataTable();
        DataTable dataConduitsLoad = new DataTable();

        DataTable elementsAdded = new DataTable();

        public ConduitsView()
        {
            InitializeComponent();

            //set datatable to add elements to grid

            elementsAdded.Rows.Clear();

            elementsAdded.Columns.Add("Numb", typeof(int));
            elementsAdded.Columns.Add("Level", typeof(string));
            elementsAdded.Columns.Add("Conductors", typeof(string));
            elementsAdded.Columns.Add("QtConductors", typeof(string));
            elementsAdded.Columns.Add("Size", typeof(string));
            elementsAdded.Columns.Add("Triplex", typeof(bool));
            elementsAdded.Columns.Add("Ground", typeof(string));

            CircuitsDataGrid.ItemsSource = elementsAdded.DefaultView;


            List<string> conduitTypes = new List<string>();
            conduitTypes.Add("EMT - Electrical Metallic Tubing");
            conduitTypes.Add("ENT - Electrical Nonmetallic Tubing");
            conduitTypes.Add("FMC - Flexible Metal Conduit");
            conduitTypes.Add("IMC - Intermediate Metal Conduit");
            conduitTypes.Add("RMC - Rigid Metal Conduit");
            conduitTypes.Add("PVC - Rigid PVC Conduit - SCH 80");
            conduitTypes.Add("PVC/HDPE - Rigid PVC Conduit - SCH 40");

            cmbCondType.ItemsSource = conduitTypes;
            




        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // Show loading indicator
            LoadingIndicator.Visibility = Visibility.Visible;
            ContentGrid.IsEnabled = false;

            try
            {
                // Load data asynchronously to avoid blocking UI
                await Task.Run(() =>
                {
                    databaseLoad = acessos.ExecuteQuery("SELECT * FROM cables");
                    dataConduitsLoad = acessos.ExecuteQuery("SELECT * FROM conduitsSizes");
                });

                // Fills the first combo box with all distinct Level values
                var distinctLevels = databaseLoad.AsEnumerable()
                                    .Select(row => row.Field<string>("Level"))
                                    .Where(level => !string.IsNullOrEmpty(level) && level != "GND")
                                    .Distinct()
                                    .ToList();

                cmbLevel.ItemsSource = distinctLevels;
            }
            finally
            {

                // Hide loading indicator
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ContentGrid.IsEnabled = true;
                // starts with the label and grounding size hidden
                lblGround.IsEnabled = false;
                cmbGround.IsEnabled = false;
            }
        }

        private void cmbLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clears the dependent combo boxes
            LimparCombosSequenciais(1);

            if (cmbLevel.SelectedItem != null)
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();

                var tiposDistintos = databaseLoad.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == levelSelecionado)
                                    .Select(row => row.Field<string>("Type"))
                                    .Where(type => !string.IsNullOrEmpty(type))
                                    .Distinct()
                                    .ToList();

                cmbType.ItemsSource = tiposDistintos;
                Debug.WriteLine($"Level: '{levelSelecionado}' - Types encontrados: {tiposDistintos.Count}");
            }
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clears the dependent combo boxes
            LimparCombosSequenciais(2);

            if (cmbLevel.SelectedItem != null && cmbType.SelectedItem != null)
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();

                var cablesDistintos = databaseLoad.AsEnumerable()
                                     .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                  row.Field<string>("Type") == typeSelecionado)
                                     .Select(row => row.Field<string>("Cables"))
                                     .Where(cables => !string.IsNullOrEmpty(cables))
                                     .Distinct()
                                     .ToList();

                cmbConductors.ItemsSource = cablesDistintos;
                Debug.WriteLine($"Level: '{levelSelecionado}', Type: '{typeSelecionado}' - Cables encontrados: {cablesDistintos.Count}");
            }
        }

        private void cmbConductors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clears the dependent combo boxes
            LimparCombosSequenciais(3);

            // GIF logic - Shows when "Single" is selected
            if (cmbConductors.SelectedItem != null && cmbConductors.SelectedItem.ToString() == "Single")
            {
                checkTriplex.IsEnabled = true;
                checkTriplex.IsChecked = true;

                lblGround.IsEnabled = true;
                cmbGround.IsEnabled = true;

                string levelSelecionado = "600V";
                string typeSelecionado = "Power";
                string cables = "Single";

                var distinctGrounds = databaseLoad.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == "GND")
                                    .Select(row => row.Field<string>("Size"))
                                    .Where(cables => !string.IsNullOrEmpty(cables))
                                    .Distinct()
                                    .ToList();

                cmbGround.ItemsSource = distinctGrounds;


            }
            else
            {
                checkTriplex.IsEnabled = false;
                checkTriplex.IsChecked = false;
            }

            if (TemSelecaoCompleta(3))
            {
                ProcessarAmountMulti();
                ProcessarSize();
            }
        }



        private void cmbAmountMulti_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reprocesses Size when Amount Multi changes (if it's Multiconductor)
            if (cmbConductors.SelectedItem?.ToString() == "Multiconductor" && cmbAmountMulti.SelectedItem != null)
            {
                ProcessarSizeComAmountMulti();
            }
        }

        private void ProcessarAmountMulti()
        {
            string conductorsSelecionado = cmbConductors.SelectedItem.ToString();

            if (conductorsSelecionado == "Multiconductor")
            {
                // If Multiconductor, fills with values from the Conductors column
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();

                var quantidadeCondutores = databaseLoad.AsEnumerable()
                                          .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                       row.Field<string>("Type") == typeSelecionado &&
                                                       row.Field<string>("Cables") == conductorsSelecionado)
                                          .Select(row => row.Field<string>("Conductors"))
                                          .Where(conductors => !string.IsNullOrEmpty(conductors))
                                          .Distinct()
                                          .ToList();

                cmbAmountMulti.ItemsSource = quantidadeCondutores;
                Debug.WriteLine($"Multiconductor - Quantidades encontradas: {quantidadeCondutores.Count}");
            }
            else
            {
                // If not Multiconductor, just the value "1"
                cmbAmountMulti.ItemsSource = new List<string> { "1" };
                cmbAmountMulti.SelectedIndex = 0;
                Debug.WriteLine("Não é Multiconductor - Valor fixo: 1");
            }
        }

        private void ProcessarSize()
        {
            if (TemSelecaoCompleta(3))
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();
                string cablesSelecionado = cmbConductors.SelectedItem.ToString();

                var sizesDistintos = databaseLoad.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                 row.Field<string>("Type") == typeSelecionado &&
                                                 row.Field<string>("Cables") == cablesSelecionado)
                                    .Select(row => row.Field<string>("Size"))
                                    .Where(size => !string.IsNullOrEmpty(size))
                                    .Distinct()
                                    .ToList();

                cmbSize.ItemsSource = sizesDistintos;
                Debug.WriteLine($"Sizes encontrados: {sizesDistintos.Count}");

                foreach (var size in sizesDistintos)
                {
                    Debug.WriteLine($"- Size: '{size}'");
                }
            }
        }

        private void ProcessarSizeComAmountMulti()
        {
            if (TemSelecaoCompleta(3) && cmbAmountMulti.SelectedItem != null)
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();
                string cablesSelecionado = cmbConductors.SelectedItem.ToString();
                string amountSelecionado = cmbAmountMulti.SelectedItem.ToString();

                var sizesDistintos = databaseLoad.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                 row.Field<string>("Type") == typeSelecionado &&
                                                 row.Field<string>("Cables") == cablesSelecionado &&
                                                 row.Field<string>("Conductors") == amountSelecionado)
                                    .Select(row => row.Field<string>("Size"))
                                    .Where(size => !string.IsNullOrEmpty(size))
                                    .Distinct()
                                    .ToList();

                cmbSize.ItemsSource = sizesDistintos;
                Debug.WriteLine($"Sizes para Multiconductor ({amountSelecionado} condutores): {sizesDistintos.Count}");
            }
        }

        // Helper methods
        private void LimparCombosSequenciais(int nivel)
        {
            switch (nivel)
            {
                case 1: // Clears everything after Level
                    cmbType.ItemsSource = null;
                    cmbType.SelectedIndex = -1;
                    
                    goto case 2;
                case 2: // Clears everything after Type
                    cmbConductors.ItemsSource = null;
                    cmbConductors.SelectedIndex = -1;
                    cmbGround.SelectedIndex = -1;
                    cmbGround.IsEnabled = false;
                    goto case 3;
                case 3: // Clears everything after Conductors
                    cmbAmountMulti.ItemsSource = null;
                    cmbAmountMulti.SelectedIndex = -1;
                    cmbSize.ItemsSource = null;
                    cmbSize.SelectedIndex = -1;
                    cmbGround.SelectedIndex = -1;
                    cmbGround.IsEnabled = false;
                    break;
            }
        }

        private bool TemSelecaoCompleta(int nivel)
        {
            switch (nivel)
            {
                case 3:
                    return cmbLevel.SelectedItem != null &&
                           cmbType.SelectedItem != null &&
                           cmbConductors.SelectedItem != null;
                case 4:
                    return TemSelecaoCompleta(3) && cmbAmountMulti.SelectedItem != null;
                default:
                    return false;
            }
        }

        
        private void btnAddCircuit_Click(object sender, RoutedEventArgs e)
        {
            DataRow newRow = elementsAdded.NewRow();

            // Fill column values
            newRow["Numb"] = elementsAdded.Rows.Count + 1;
            newRow["Level"] = cmbLevel.SelectedItem?.ToString() ?? string.Empty;
            newRow["Conductors"] = cmbConductors.SelectedItem?.ToString() ?? string.Empty;
            newRow["QtConductors"] = cmbAmountMulti.SelectedItem?.ToString() ?? string.Empty;
            newRow["Size"] = cmbSize.SelectedItem?.ToString() ?? string.Empty;
            newRow["Triplex"] = checkTriplex.IsChecked;
            newRow["Ground"] = cmbGround.SelectedItem?.ToString() ?? string.Empty;

            // Add the row to the table
            elementsAdded.Rows.Add(newRow);

            
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button?.Tag is DataRowView rowView)
            {
                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    "Tem certeza que deseja excluir esta linha?",
                    "Confirmar Exclusão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Removes from DataTable (the DataGrid will be updated automatically)
                    rowView.Row.Delete();
                    elementsAdded.AcceptChanges();
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            elementsAdded.Rows.Clear();
        }

        private void btnSize_Click(object sender, RoutedEventArgs e)
        {
            //size the conduit
        }
    }
}