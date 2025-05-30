using System.Data;
using System.Diagnostics;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Windows; // For RoutedEventArgs, Visibility
using System.Threading.Tasks; // For Task
using System; // For Exception

namespace WpfApp2
{
    public partial class ConduitsView : UserControl
    {
        public DatabaseAccess acessos = new DatabaseAccess();
        DataTable databaseLoad = new DataTable(); // Will be loaded async

        public ConduitsView()
        {
            InitializeComponent();
            // Synchronous load removed
        }

        // This is the new async version
        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            ContentGrid.IsEnabled = false; // Disable UI

            try
            {
                databaseLoad = await Task.Run(() => acessos.ExecuteQuery("SELECT * FROM cables"));
                Debug.WriteLine($"Total de registros carregados: {databaseLoad.Rows.Count}");

                // Original logic from Grid_Loaded to populate cmbLevel (must run on UI thread)
                if (databaseLoad != null) // Check if databaseLoad is not null
                {
                    var distinctLevels = databaseLoad.AsEnumerable()
                                        .Select(row => row.Field<string>("Level"))
                                        .Where(level => !string.IsNullOrEmpty(level))
                                        .Distinct()
                                        .ToList();

                    cmbLevel.ItemsSource = distinctLevels;
                    Debug.WriteLine($"Levels carregados: {distinctLevels.Count}");
                }
                else
                {
                    Debug.WriteLine("Database load returned null in Grid_Loaded.");
                    // Optionally: show a message to the user in a TextBlock, e.g.
                    // ErrorMessageTextBlock.Text = "Failed to load initial data.";
                    // ErrorMessageTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading initial data in ConduitsView: {ex.Message}");
                // Optionally: show a message to the user
                // ErrorMessageTextBlock.Text = "An error occurred while loading data.";
                // ErrorMessageTextBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ContentGrid.IsEnabled = true; // Re-enable UI
            }
        }

        private void cmbLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Placeholder for user's original logic
            LimparCombosSequenciais("Level");
            if (cmbLevel.SelectedItem != null)
            {
                string selectedLevel = cmbLevel.SelectedItem.ToString();
                var distinctTypes = databaseLoad.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == selectedLevel && !string.IsNullOrEmpty(row.Field<string>("Type")))
                                    .Select(row => row.Field<string>("Type"))
                                    .Distinct()
                                    .ToList();
                cmbType.ItemsSource = distinctTypes;
            }
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Placeholder for user's original logic
            LimparCombosSequenciais("Type");
            if (cmbType.SelectedItem != null)
            {
                string selectedLevel = cmbLevel.SelectedItem?.ToString();
                string selectedType = cmbType.SelectedItem.ToString();
                var distinctConductors = databaseLoad.AsEnumerable()
                                        .Where(row => row.Field<string>("Level") == selectedLevel &&
                                                      row.Field<string>("Type") == selectedType &&
                                                      !string.IsNullOrEmpty(row.Field<string>("Conductors")))
                                        .Select(row => row.Field<string>("Conductors"))
                                        .Distinct()
                                        .ToList();
                cmbConductors.ItemsSource = distinctConductors;
            }
        }

        private void cmbConductors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Placeholder for user's original logic
            LimparCombosSequenciais("Conductors");
             if (cmbConductors.SelectedItem != null && cmbConductors.SelectedItem.ToString() == "Multiconductor")
            {
                cmbAmountMulti.Visibility = Visibility.Visible; // Show Qty
                // Populate cmbAmountMulti - assuming 'Amount_Multi' is a column
                var distinctAmountMulti = databaseLoad.AsEnumerable()
                                            .Where(row => row.Field<string>("Level") == cmbLevel.SelectedItem.ToString() &&
                                                          row.Field<string>("Type") == cmbType.SelectedItem.ToString() &&
                                                          row.Field<string>("Conductors") == "Multiconductor" &&
                                                          row.Field<string>("Amount_Multi") != null) // Ensure not null
                                            .Select(row => row.Field<string>("Amount_Multi"))
                                            .Distinct()
                                            .ToList();
                cmbAmountMulti.ItemsSource = distinctAmountMulti;
            }
            else
            {
                cmbAmountMulti.Visibility = Visibility.Collapsed; // Hide Qty
                cmbAmountMulti.ItemsSource = null; // Clear items
            }

            // Populate Size based on previous selections
            if (cmbLevel.SelectedItem != null && cmbType.SelectedItem != null && cmbConductors.SelectedItem != null)
            {
                string selectedLevel = cmbLevel.SelectedItem.ToString();
                string selectedType = cmbType.SelectedItem.ToString();
                string selectedConductors = cmbConductors.SelectedItem.ToString();

                var query = databaseLoad.AsEnumerable()
                                .Where(row => row.Field<string>("Level") == selectedLevel &&
                                              row.Field<string>("Type") == selectedType &&
                                              row.Field<string>("Conductors") == selectedConductors);

                // If Multiconductor and Qty is selected, filter by Qty as well
                if (selectedConductors == "Multiconductor" && cmbAmountMulti.SelectedItem != null)
                {
                    string selectedAmountMulti = cmbAmountMulti.SelectedItem.ToString();
                    query = query.Where(row => row.Field<string>("Amount_Multi") == selectedAmountMulti);
                }

                var distinctSizes = query.Where(row => !string.IsNullOrEmpty(row.Field<string>("Size")))
                                     .Select(row => row.Field<string>("Size"))
                                     .Distinct()
                                     .ToList();
                cmbSize.ItemsSource = distinctSizes;
            }
        }

        private void cmbAmountMulti_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Placeholder for user's original logic
             LimparCombosSequenciais("AmountMulti");
            // Logic to repopulate Size when AmountMulti changes for Multiconductor
            if (cmbConductors.SelectedItem?.ToString() == "Multiconductor" && cmbAmountMulti.SelectedItem != null)
            {
                string selectedLevel = cmbLevel.SelectedItem.ToString();
                string selectedType = cmbType.SelectedItem.ToString();
                string selectedConductors = "Multiconductor";
                string selectedAmountMulti = cmbAmountMulti.SelectedItem.ToString();

                var distinctSizes = databaseLoad.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == selectedLevel &&
                                                  row.Field<string>("Type") == selectedType &&
                                                  row.Field<string>("Conductors") == selectedConductors &&
                                                  row.Field<string>("Amount_Multi") == selectedAmountMulti &&
                                                  !string.IsNullOrEmpty(row.Field<string>("Size")))
                                    .Select(row => row.Field<string>("Size"))
                                    .Distinct()
                                    .ToList();
                cmbSize.ItemsSource = distinctSizes;
            }
        }


        private void LimparCombosSequenciais(string currentComboName)
        {
            // Placeholder for user's original logic
            if (currentComboName == "Level")
            {
                cmbType.ItemsSource = null;
                cmbConductors.ItemsSource = null;
                cmbAmountMulti.ItemsSource = null;
                cmbAmountMulti.Visibility = Visibility.Collapsed;
                cmbSize.ItemsSource = null;
            }
            else if (currentComboName == "Type")
            {
                cmbConductors.ItemsSource = null;
                cmbAmountMulti.ItemsSource = null;
                cmbAmountMulti.Visibility = Visibility.Collapsed;
                cmbSize.ItemsSource = null;
            }
            else if (currentComboName == "Conductors")
            {
                 if (cmbConductors.SelectedItem?.ToString() != "Multiconductor")
                {
                    cmbAmountMulti.ItemsSource = null;
                    cmbAmountMulti.Visibility = Visibility.Collapsed;
                }
                cmbSize.ItemsSource = null;
            }
            else if (currentComboName == "AmountMulti")
            {
                cmbSize.ItemsSource = null;
            }
        }
    }
}
