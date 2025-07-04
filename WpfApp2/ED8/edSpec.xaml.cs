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
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using MotorSpecifications;
using WireCodeConverter;
using MaterialDesignThemes.Wpf;


namespace WpfApp2.ED8
{
    /// <summary>
    /// Interaction logic for edSpec.xaml
    /// </summary>
    public partial class edSpec : UserControl
    {
        List<string> fillComboMotorvalues = new();
        public edSpec()
        {
            InitializeComponent();

            fillComboMotorvalues = ObterMotorHP();

            if (fillComboMotorvalues.Count > 0)
            {
                foreach (var item in fillComboMotorvalues)
                {
                    cmbMotorPower.Items.Add(item);
                }
            }
            else
            {
                MessageBox.Show("Nenhum valor de motor encontrado no arquivo JSON.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            MySnackbar.MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

        }
        public class MotorSpecification
        {
            public string motor_hp { get; set; }
            public double motor_curr_x { get; set; }
            public int starter_size_nema { get; set; }
            public int cb_frame_amps { get; set; }
            public int cb_trip_amps { get; set; }
            public string ol_heater_ge { get; set; }
            public string ol_heater_ch { get; set; }
            public string wire_size_phase { get; set; }
            public string conduit_size_pvc { get; set; }
            public string conduit_size_r_metal { get; set; }
        }

        public class MotorSpecRoot
        {
            public List<MotorSpecification> motor_specifications { get; set; }
        }

        public List<string> ObterMotorHP()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ED8", "motor_specifications.json");

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("Arquivo motor_specifications.json não encontrado.", jsonPath);

            string jsonContent = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<MotorSpecRoot>(jsonContent, options);

            return root.motor_specifications
                       .Select(m => m.motor_hp)
                       .Distinct()
                       .ToList();
        }
        string sizedCable = "";
        private void btnFindCable_Click(object sender, RoutedEventArgs e)
        {
            var motorManager = new MotorSpecificationManager();
            List<string> specs = motorManager.GetMotorSpecifications(cmbMotorPower.SelectedItem.ToString());

            specs[1] = SimpleWireConverter.ConvertWireCode(specs[1].ToString());
            sizedCable = specs[1];

            txtMultiline.Text = "I = " +  specs[0].ToString() + "\n\r" +
                                "Cable Size = " + specs[1].ToString() + "\n\r" +
                                "PVC Conduit = " + specs[2].ToString() + "\n\r" +
                                "Metalic Conduit = " + specs[3].ToString();

            


        }

        private void btnClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(sizedCable);
            MySnackbar.MessageQueue?.Enqueue("Copy to Clipboard");
        }

        private void btnClipboard_Click_1(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(sizedCable);
            MySnackbar.MessageQueue?.Enqueue("Copy to Clipboard");
        }
    }
}
