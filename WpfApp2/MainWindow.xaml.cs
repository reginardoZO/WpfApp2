using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf; // Add this line to use DrawerHost

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;

            // Loads HomeView (Lorem Ipsum) on application startup
            MainContentHost.Content = new HomeView(); // This line was effectively already present
            TitleTextBlock.Text = "Início"; // Set title for the initial HomeView
        }

        // Method to handle sidebar menu button clicks
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                string menuOption = clickedButton.Tag.ToString();

                switch (menuOption)
                {
                    case "Calculadora":
                        MainContentHost.Content = new CalculadoraView();
                        TitleTextBlock.Text = "Calculadora";
                        break;
                    case "Inicio":
                        MainContentHost.Content = new HomeView();
                        TitleTextBlock.Text = "Início";
                        break;
                    case "Conduits":
                        MainContentHost.Content = new ConduitsView();
                        TitleTextBlock.Text = "Conduits";
                        break;
                    case "Util":
                        MainContentHost.Content = new UtilView();
                        TitleTextBlock.Text = "Util";
                        break;
                        // Add more 'case' statements here for each new menu button you create
                }

                MenuToggleButton.IsChecked = false; // This will close the drawer
                MainContentHost.Focus(); // Set focus to the content area
            }
        }
    }
}
