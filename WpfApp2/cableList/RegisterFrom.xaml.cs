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

namespace WpfApp2.cableList
{
    /// <summary>
    /// Interaction logic for RegisterFrom.xaml
    /// </summary>
    public partial class RegisterFrom : Window
    {
        string projeto;
        string planta;
        string tipo;
        public RegisterFrom()
        {
            InitializeComponent();
            
        }

        public RegisterFrom(string textoRecebido)
        {
            InitializeComponent();
            string[] separado = textoRecebido.Split('|');
            projeto = separado[0];
            planta = separado[1];
            tipo = separado[2];

            if(tipo == "Panel")
            {
                addFromPanelName.Visibility = Visibility.Visible;
                addFromPanelName.projeto = projeto;
                addFromPanelName.planta = planta;
                addFromPanelName.tipo = tipo;
            }
            if(tipo == "Transformer")
            {
                addFromTransformer.Visibility = Visibility.Visible;
                addFromTransformer.projeto = projeto;
                addFromTransformer.planta = planta;
                addFromTransformer.tipo = tipo;

            }
          

       
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void addFromPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
