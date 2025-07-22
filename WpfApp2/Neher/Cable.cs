using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Neher
{
    public class Cable
    {

        public double alfaDoCobre { get; set; }
        public double LF { get; set; }
        public double Rdc_TC_ft { get;set; }
        public double Rdc_TC { get; set; }

        public double Rdc_25C { get; set; }

        public double Rac_75 {  get; set; }

        public double Rac_90 {  get; set; }

        public double Rdc_75 { get; set; }

        public double Rdc_90 { get; set; }

        public double Xs {  get; set; }

        public double Y_skin {  get; set; }

        public double TC { get; set; }

        public string size { get; set; }

        public double d_c { get; set; }

        public double s { get; set; }

        public double voltage { get; set; }

        public double Y_prox { get;set; }

        public double Y_c { get;set; }

        public double Rac { get; set; }

        public double Rac_m { get; set; }


        public double Wd { get; set; }

        public double soilTemp { get; set; }
        public double R_air { get; set; }

        public double R_duct { get; set; }

        public double D_o_duct { get; set; }

        public double D_i_duct { get; set; }

        public double conduitSize { get; set; }

        public double R_earth { get; set; }

        public double rho_soil { get; set; }

        public double H { get; set; }

        public double R_ext { get;set; }

        public double deltaTD { get; set; }

        public double R_Ins { get; set; }

        public double RCA { get; set; }

        public double I { get; set; }

        public double Y_sh { get; set; }

        public double R_sh { get; set; }

        public double F_sh { get; set; }

        public double T_surface { get; set; }

        public double theta_m { get;set; }
        public double RhoInsCm { get; set; } = 350.0; // Padrão para XLPE/EPR; °C·cm/W. Torne input no UI.
        public double RhoDuctCm { get; set; } = 650.0; // Padrão para PVC; °C·cm/W. Já é input (txtRhoDuct).

    }
}
