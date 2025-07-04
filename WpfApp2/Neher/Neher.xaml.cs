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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;

namespace WpfApp2.Neher
{
    /// <summary>
    /// Interaction logic for Neher.xaml
    /// </summary>
    public partial class Neher : UserControl
    {

        DatabaseAccess acessos = new();

        NeherElement elementoNeher = new();

        NeherCalc calc = new();
        Cable cable = new();
        public Neher()
        {

            InitializeComponent();

            comboLevel.Items.Add("Low Voltage");
            comboLevel.Items.Add("VFD");
            comboLevel.Items.Add("Medium Voltage");
            comboLevel.SelectedIndex = -1;
            DataTable conduites = acessos.ExecuteQuery("select Size from conduitsNeher");
            cmbConduit.ItemsSource = conduites.DefaultView;
            cmbConduit.DisplayMemberPath = "Size";
            cmbConduit.SelectedValuePath = "Size"; // Adicione esta linha!

        }

        private void comboLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cable.type = comboLevel.SelectedItem.ToString();

            string columnToSearch = "";

            switch (cable.type)
            {
                case "Low Voltage":
                    cable.columnToSearch = "single";
                    break;
                case "VFD":
                    cable.columnToSearch = "vfd";
                    break;
                case "Medium Voltage":
                    cable.columnToSearch = "media";
                    break;
                default:
                    cable.columnToSearch = "";
                    break;
            }

            string varSql = $"select distinct size from cablesNeher where source_file = '{cable.columnToSearch}'";

            DataTable resposta = acessos.ExecuteQuery(varSql);
            cmbCable.Items.Clear();
            if (resposta != null)
            {

                foreach (DataRow linha in resposta.Rows)
                    cmbCable.Items.Add(linha[0]);
                cmbCable.SelectedIndex = -1;
            }


        }

        private void cmbCable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            // Sizing Skin Effect

            cable.size = cmbCable.SelectedValue.ToString();

            string varSql = $"Select dc_resistance_25c_ohm_1000ft from cablesNeher where source_file = '{cable.columnToSearch}' and size = '{cable.size}'";

            DataTable resposta = acessos.ExecuteQuery(varSql);

            cable.Rdc_25C = Convert.ToDouble(resposta.Rows[0][0].ToString());

            (elementoNeher.Xs, elementoNeher.Ys, elementoNeher.Rdc_Tc) = calc.ysCalc(cable);
            

            // Sizing Proximity Effect

            varSql = $"Select diameter_over_conductor_inch, approx_od_inch from cablesNeher where source_file = '{cable.columnToSearch}' and size = '{cable.size}'";

            resposta = acessos.ExecuteQuery(varSql);

            cable.diamCond = Convert.ToDouble(resposta.Rows[0][0].ToString());

            cable.OD = Convert.ToDouble(resposta.Rows[0][1].ToString());


            (elementoNeher.Kp, elementoNeher.Yp) = calc.ypCalc(cable, elementoNeher);

            elementoNeher.Yc = elementoNeher.Ys + elementoNeher.Yp;

            elementoNeher.Rac = elementoNeher.Rdc_Tc * (1 + elementoNeher.Yc);

            txttrash.Text = elementoNeher.Rac.ToString();

            //Sizing Insulation Resistance

            varSql = $"select insul_thickness_mil from cablesNeher where source_file = '{cable.columnToSearch}' and size = '{cable.size}'";

            resposta = acessos.ExecuteQuery(varSql);

            cable.sizeInsul = Convert.ToDouble(resposta.Rows[0][0].ToString());

            elementoNeher.Ri = calc.riCalc(cable);

            // Sizing Thermal Resistence Inside the Conduit

            // Multicable é so o tipo VFD - os demais sao trifólior

            if (cable.type == "Low Voltage" || cable.type == "Medium Voltage")
                elementoNeher.Ds = 2.155 * (cable.diamCond + 2*(cable.sizeInsul/1000));
            else
                elementoNeher.Ds = cable.OD;


            elementoNeher.Rsd = 6.864 / elementoNeher.Ds;



            txttrash.Text = elementoNeher.Rsd.ToString();

        }

        private void cmbConduit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //sizing Rd

            string varsql = $"select Average_OD_in, SCH40_Minimum_Wall from conduitsNeher  where size = '{cmbConduit.SelectedValue.ToString()}'";

            DataTable resposta = acessos.ExecuteQuery(varsql);
            
            double Dod = Convert.ToDouble(resposta.Rows[0][0].ToString()); //Average OD 
            double dt = Convert.ToDouble(resposta.Rows[0][1].ToString()); //Minimum Wall

            cable.conduitDod = Dod;
            cable.conduitWall = dt;

            elementoNeher.Rd = 6 * Math.Log(Dod/(Dod - 2 * dt));

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            double ps = Convert.ToDouble(txtPs.Text.ToString());
            double h = Convert.ToDouble(txtProf.Text.ToString() );
            elementoNeher.Re_m = calc.CalcularReMutuo(ps, h, cable.conduitDod, Convert.ToDouble(txtDistCenterCenter.Text.ToString()), Convert.ToInt16(txtqtCond.Text.ToString()), Convert.ToInt16(txtPosition.Text.ToString()));

            txttrash.Text = elementoNeher.Re_m.ToString();

            elementoNeher.Re = calc.CalcularRe(ps, h, cable.conduitDod);

            elementoNeher.Rca = elementoNeher.Ri + elementoNeher.Rsd + elementoNeher.Rd + elementoNeher.Re + elementoNeher.Re_m;

            string resltado = $"Ri = {elementoNeher.Ri}\n" +
                 $"Rsd = {elementoNeher.Rsd}\n" +
                 $"Rd = {elementoNeher.Rd}\n" +
                 $"Re = {elementoNeher.Re}\n" +
                 $"Re_m = {elementoNeher.Re_m}\n" +
                 $"Rca = {elementoNeher.Rca}";


            double IcorrenteAdmissivel = calc.CalcularCorrenteAdmissivel(Convert.ToDouble(comboLevel.SelectedValue.ToString() == "Medium Voltage" ? 105.0 : 90.0), Convert.ToDouble(txtAmbTemp.Text.ToString()), 0.0, 3, elementoNeher.Rac, elementoNeher.Rca);

            txttrash.Text = IcorrenteAdmissivel.ToString();
        }
    }
}

