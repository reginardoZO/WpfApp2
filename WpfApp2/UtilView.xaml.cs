using System.Data;
using System.Windows.Controls;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for UtilView.xaml
    /// </summary>
    public partial class UtilView : UserControl

    {
        DatabaseAccess acessos = new();
        public UtilView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string varSql = $"Select \"{cmbTemp.Text}\", size from nec_310_16";
            string outroSql = $"select \"{cmbTemp.Text}\", size from nec_310_17";
            string sqlGround = $"select Current, Copper from nec_250_122";

            DataTable tabela16 = acessos.ExecuteQuery(varSql);
            DataTable tabela17 = acessos.ExecuteQuery(outroSql);
            DataTable tabela22 = acessos.ExecuteQuery(sqlGround);

            double correnteEntrada = Convert.ToDouble(txtCurrent.Text);

            double correnteDoBanco;

            foreach (DataRow linha in tabela16.Rows)
            {
                correnteDoBanco = Convert.ToDouble(linha[0].ToString());

                if (correnteDoBanco > correnteEntrada)
                {
                    lbl1016.Content = linha[1].ToString();

                    break;
                }
            }

            foreach (DataRow linha in tabela17.Rows)
            {
                correnteDoBanco = Convert.ToDouble(linha[0].ToString());

                if (correnteDoBanco > correnteEntrada)
                {
                    lbl1017.Content = linha[1].ToString();

                    break;
                }
            }

            foreach (DataRow linha in tabela22.Rows)
            {
                correnteDoBanco = Convert.ToDouble(linha[0].ToString());

                if (correnteDoBanco > correnteEntrada)
                {
                    lblgrounding.Content = linha[1].ToString(); 
                    break;
                }
            }


        }
    }
}
