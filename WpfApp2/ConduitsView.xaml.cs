using System.Data;
using System.Diagnostics;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for ConduitsView.xaml
    /// </summary>
    public partial class ConduitsView : UserControl
    {
        public DatabaseAccess acessos = new DatabaseAccess();
        DataTable databaseLoad = new DataTable();

        public ConduitsView()
        {
            InitializeComponent();
            // Carrega todo o banco uma única vez
            databaseLoad = acessos.ExecuteQuery("SELECT * FROM cables");
        }

        private void Grid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Preenche o primeiro combo com todos os valores distintos de Level
            var distinctLevels = databaseLoad.AsEnumerable()
                                .Select(row => row.Field<string>("Level"))
                                .Where(level => !string.IsNullOrEmpty(level))
                                .Distinct()
                                .ToList();

            cmbLevel.ItemsSource = distinctLevels;
        }

        private void cmbLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpa os combos dependentes
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
            }
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpa os combos dependentes
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
            }
        }

        private void cmbConductors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpa os combos dependentes
            LimparCombosSequenciais(3);

            if (TemSelecaoCompleta(3))
            {
                ProcessarAmountMulti();
                ProcessarSize();
            }
        }

        private void ProcessarAmountMulti()
        {
            string conductorsSelecionado = cmbConductors.SelectedItem.ToString();

            if (conductorsSelecionado == "Multiconductor")
            {
                // Se for Multiconductor, preenche com valores da coluna Conductors
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
            }
            else
            {
                // Se não for Multiconductor, apenas o valor "1"
                cmbAmountMulti.ItemsSource = new List<string> { "1" };
                cmbAmountMulti.SelectedIndex = 0; // Seleciona automaticamente o "1"
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

        private void cmbAmountMulti_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reprocessa o Size quando Amount Multi muda (se for Multiconductor)
            if (cmbConductors.SelectedItem?.ToString() == "Multiconductor" && cmbAmountMulti.SelectedItem != null)
            {
                ProcessarSizeComAmountMulti();
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
            }
        }

        // Métodos auxiliares
        private void LimparCombosSequenciais(int nivel)
        {
            switch (nivel)
            {
                case 1: // Limpa tudo após Level
                    cmbType.ItemsSource = null;
                    cmbType.SelectedIndex = -1;
                    goto case 2;
                case 2: // Limpa tudo após Type
                    cmbConductors.ItemsSource = null;
                    cmbConductors.SelectedIndex = -1;
                    goto case 3;
                case 3: // Limpa tudo após Conductors
                    cmbAmountMulti.ItemsSource = null;
                    cmbAmountMulti.SelectedIndex = -1;
                    cmbSize.ItemsSource = null;
                    cmbSize.SelectedIndex = -1;
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
    }
}