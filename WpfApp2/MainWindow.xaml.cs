using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf; // Adicione esta linha para usar o DrawerHost
using WpfApp2.Conduits; // Certifique-se de que o namespace esteja correto
using WpfApp2.Projects;

namespace WpfApp2
{
    /// <summary>
    /// Lógica de interação para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;

            // Carrega o HomeView (Lorem Ipsum) na inicialização da aplicação
            MainContentHost.Content = new HomeView(); // This line was effectively already present
            TitleTextBlock.Text = ""; // Set title for the initial HomeView
        }

        // Método para lidar com o clique dos botões do menu lateral
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
                        TitleTextBlock.Text = "";
                        break;
                    case "Conduits":
                        MainContentHost.Content = new ConduitsView();
                        TitleTextBlock.Text = "Conduits";
                        break;
                    case "Util":
                        MainContentHost.Content = new UtilView();
                        TitleTextBlock.Text = "Util";
                        break;
                    case "Projects":
                        MainContentHost.Content = new Projects.Projects(); // use nome completo se necessário
                        TitleTextBlock.Text = "Projects";
                        break;
                        // Adicione mais 'case' aqui para cada novo botão de menu que você criar
                }

                MenuToggleButton.IsChecked = false; // This will close the drawer
                MainContentHost.Focus(); // Set focus to the content area
            }
        }
    }
}
