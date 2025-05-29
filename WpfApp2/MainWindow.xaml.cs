using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf; // Adicione esta linha para usar o DrawerHost

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

            // Carrega o HomeView (Lorem Ipsum) na inicialização da aplicação
            MainContentHost.Content = new HomeView();
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
                        // Adicione mais 'case' aqui para cada novo botão de menu que você criar
                }

                MenuToggleButton.IsChecked = false; // This will close the drawer
                MainContentHost.Focus(); // Set focus to the content area
            }
        }
    }
}
