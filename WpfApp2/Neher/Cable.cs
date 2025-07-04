using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Neher
{
    internal class Cable
    {
        public string columnToSearch {  get; set; }
        public string type { get; set; }
        public double Rdc_25C { get; set; }

        public string size {  get; set; }

        public double diamCond { get; set; }

        public double OD { get; set; }

        public double sizeInsul { get; set; }

        public double conduitDod { get; set; }

        public double conduitWall { get; set; }

        
    }
}
