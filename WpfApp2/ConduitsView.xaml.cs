using System.Data;
using System.Diagnostics;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System; // For Exception
using System.Windows.Threading; // Added for DispatcherTimer
using System.Windows;
using System.Threading.Tasks;


namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for ConduitsView.xaml
    /// </summary>
    public partial class ConduitsView : UserControl
    {
        public DatabaseAccess acessos = new DatabaseAccess();
        

        DataTable databaseLoad = new DataTable(); // Will be loaded async
        private System.Windows.Threading.DispatcherTimer gifTimer;



        public ConduitsView()
        {
            InitializeComponent();


            gifTimer = new System.Windows.Threading.DispatcherTimer();
            gifTimer.Interval = TimeSpan.FromSeconds(2);
            gifTimer.Tick += GifTimer_Tick;

        }

        private async void Grid_Loaded(object sender, System.Windows.RoutedEventArgs e)
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
                });

                Debug.WriteLine($"Total de registros carregados: {databaseLoad.Rows.Count}");

                // Preenche o primeiro combo com todos os valores distintos de Level
                var distinctLevels = databaseLoad.AsEnumerable()
                                    .Select(row => row.Field<string>("Level"))
                                    .Where(level => !string.IsNullOrEmpty(level))
                                    .Distinct()
                                    .ToList();

                cmbLevel.ItemsSource = distinctLevels;
                Debug.WriteLine($"Levels carregados: {distinctLevels.Count}");
            }
            finally
            {
                // Hide loading indicator
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ContentGrid.IsEnabled = true;
            }
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
                Debug.WriteLine($"Level: '{levelSelecionado}' - Types encontrados: {tiposDistintos.Count}");
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
                Debug.WriteLine($"Level: '{levelSelecionado}', Type: '{typeSelecionado}' - Cables encontrados: {cablesDistintos.Count}");
            }
        }

        private void cmbConductors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            LimparCombosSequenciais("Conductors"); // Existing logic from user (adapted name)

            // New GIF Logic:
            if (cmbConductors.SelectedItem != null && cmbConductors.SelectedItem.ToString() == "Single")
            {
                try
                {
                    // Uri for the GIF, assuming it's a resource in the root of the project.
                    if (ArrowGifPlayer.Source == null || ArrowGifPlayer.Source.ToString() != "pack://application:,,,/arrowGIF.gif")
                    {
                       ArrowGifPlayer.Source = new Uri("pack://application:,,,/arrowGIF.gif", UriKind.Absolute);
                    }

                    ArrowGifPlayer.Position = TimeSpan.Zero; // Rewind
                    ArrowGifPlayer.Play(); // Start playing the GIF
                    ArrowGifPlayer.Visibility = Visibility.Visible; // Make it visible

                    gifTimer.Start(); // Start the 2-second timer
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error with Arrow GIF: {ex.Message}");
                    if (ArrowGifPlayer != null) ArrowGifPlayer.Visibility = Visibility.Collapsed; // Hide on error
                    gifTimer.Stop(); // Ensure timer is stopped on error
                }
            }
            else
            {
                // If selection is not "Single" or is null, ensure GIF is hidden and timer is stopped.
                gifTimer.Stop();
                if (ArrowGifPlayer != null)
                {
                    ArrowGifPlayer.Stop();
                    ArrowGifPlayer.Visibility = Visibility.Collapsed;
                    // ArrowGifPlayer.Source = null; // Optionally clear source here too
                }
            }

            // Resume existing logic from user:
            // (This part is based on the reconstructed logic from previous steps)
            if (cmbConductors.SelectedItem != null && cmbConductors.SelectedItem.ToString() == "Multiconductor")

            // Limpa os combos dependentes
            LimparCombosSequenciais(3);

            if (TemSelecaoCompleta(3))

            {
                ProcessarAmountMulti();
                ProcessarSize();
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
                Debug.WriteLine($"Multiconductor - Quantidades encontradas: {quantidadeCondutores.Count}");
            }
            else
            {
                // Se não for Multiconductor, apenas o valor "1"
                cmbAmountMulti.ItemsSource = new List<string> { "1" };
                cmbAmountMulti.SelectedIndex = 0; // Seleciona automaticamente o "1"
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

        public void GifPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // STUB METHOD TO FIX BUILD ERROR
            // This method is likely referenced in XAML but was missing.
            // If this event handling is actually needed, the logic should be implemented here.
            System.Diagnostics.Debug.WriteLine("GifPlayer_MediaEnded called - STUB");
        }

        private void GifTimer_Tick(object sender, EventArgs e)
        {
            gifTimer.Stop();
            if (ArrowGifPlayer != null) // Check if ArrowGifPlayer is not null
            {
                ArrowGifPlayer.Stop();
                ArrowGifPlayer.Visibility = Visibility.Collapsed;
                ArrowGifPlayer.Source = null; // Release the source
            }
        }
    }
}