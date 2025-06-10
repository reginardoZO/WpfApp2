using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Projects
{
    class trafo
    {
        public int Id { get; set; }
        public string High { get; set; }
        public string Low { get; set; }
        public string Tag { get; set; }
        public string Power { get; set; }
        public string FromPlant { get; set; }
        public string FromPanel { get; set; }
        public string Project { get; set; }
        public string DateAdded { get; set; }
        public string ToPlant { get; set; }
        public string ToPanel { get; set; }
        public string FromUnit { get; set; }
        public string ToUnit { get; set; }

        // Construtor padrão
        public trafo()
        {
        }

    }
}
