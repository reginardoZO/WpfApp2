using System.Data;
using System.Threading.Tasks;
using System.Windows; // For RoutedEventArgs and Visibility
using System.Windows.Controls; // For UserControl (already there)
using System; // For Exception (if more detailed error handling is added)

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for ConduitsView.xaml
    /// </summary>
    public partial class ConduitsView : UserControl
    {
        private DatabaseAccess acessos = new DatabaseAccess();
        private DataTable databaseLoad;

        public ConduitsView()
        {
            InitializeComponent();
        }

        private async void ConduitsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            CablesDataGrid.Visibility = Visibility.Collapsed;

            string query = "SELECT * FROM cables"; // As confirmed by the user

            try
            {
                databaseLoad = await Task.Run(() => acessos.ExecuteQuery(query));

                if (databaseLoad != null)
                {
                    CablesDataGrid.ItemsSource = databaseLoad.DefaultView;
                    CablesDataGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    // Optional: Display an error message to the user
                    // For example, if you add a TextBlock named ErrorTextBlock:
                    // ErrorTextBlock.Text = "Failed to load data from the database.";
                    // ErrorTextBlock.Visibility = Visibility.Visible;
                    Console.WriteLine("ConduitsView: databaseLoad returned null after query execution.");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions during the async operation or UI update
                Console.WriteLine($"Error loading conduits data: {ex.Message}");
                // Optional: Display an error message to the user
                // ErrorTextBlock.Text = "An error occurred while loading data.";
                // ErrorTextBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }
    }
}
