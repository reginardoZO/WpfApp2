using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace WpfApp2.CircuitsList
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {


        public string DadosRecebidos { get; set; }

        public PopupWindow()
        {
            InitializeComponent();
            Loaded += PopupWindow_Loaded;
        }

        private void PopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Acesso aos dados passados
            lbl.Content= DadosRecebidos;
        }
    }
}
