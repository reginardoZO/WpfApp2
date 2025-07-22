using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Neher
{
    internal class NeherCalc
    {
      
        public List<string> retornaUnidades(string carga)
        {
            List<string> unidadesRetorno = new();

            switch (carga)
            {
                case "MOTOR":
                    unidadesRetorno.Add("kW");
                    unidadesRetorno.Add("HP");
                    break;
                case "XFRM":
                    unidadesRetorno.Add("kVA");
                    
                    break;
                case "HEATER":
                    unidadesRetorno.Add("kW");
                    
                    break;
                case "FEEDER":
                    unidadesRetorno.Add("kVA");
                    
                    break;
                case "GENERATOR":
                    unidadesRetorno.Add("kVA");
                    unidadesRetorno.Add("kW");
                    unidadesRetorno.Add("HP");                    
                    break;
                
            }

            return unidadesRetorno;



        }


    }

}

