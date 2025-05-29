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
                        break;
                    case "Inicio":
                        MainContentHost.Content = new HomeView();
                        break;
                        // Adicione mais 'case' aqui para cada novo botão de menu que você criar
                }

                // Fecha o DrawerHost (menu lateral) após a seleção de uma opção
                // Isso é importante para menus "flyout" que deslizam para fora.
                // O DrawerHost.CloseDrawerCommand é um comando estático que fecha o drawer.
                // O segundo parâmetro (this) é o target do comando, que é a própria janela.
                DrawerHost.CloseDrawerCommand.Execute(null, this);
            }
        }
    }
}
