using System.Configuration;
using System.Data;
using System.Windows;
using OfficeOpenXml;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            // Configurar licença EPPlus uma única vez para toda a aplicação
            // Para uso pessoal não comercial:
            ExcelPackage.License.SetNonCommercialPersonal("Reginardo");

            // Para uso organizacional não comercial, use:
            // ExcelPackage.License.SetNonCommercialOrganization("Nome da Sua Organização");

            // Para uso comercial (se tiver licença), use:
            // ExcelPackage.License.SetCommercial("Sua Chave de Licença");
        }

    }



}
