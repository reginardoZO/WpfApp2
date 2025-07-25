using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Neher
{
    public class cableNew
    {
        public double voltageMain { get; set; }
        public double R { get; set; }
        public double Rlinha { get; set; }
        public double R0 { get; set; }
        public double Rdc_25_ohm_m { get; set; }
        public double Rdc_25_ohm_1000ft { get; set; }

        public double theta { get; set; }

        public string cableSection { get; set; }

        public double xs2 { get; set; }

        public double ys { get; set; }

        public double yp { get; set; }

        public double xp2 { get; set; }

        public double diam_in_in { get; set; }

        public double diam_in_mm { get; set; }

        public double diam_ext_in { get; set; }

        public double diam_ext_mm { get; set; }

        public double T1 { get; set; }

        public double rho_T { get; set; }

        public double t1_inches { get; set; }

        public double t1_mm { get; set; }

        public double soilTemp { get; set; }

        public double soilRes_m { get; set; }

        public double cond_ext_dim_m { get; set; }
        public double cond_int_dim_m { get; set; }

        public double ampacidade { get; set; }


    }
}
