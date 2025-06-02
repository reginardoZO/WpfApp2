using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;

namespace WpfApp2
{
    public class calc()
    {
        // conduit sizing area
        public (string sized, string calculation) sizeConduit(DataTable cableTable, DataTable conduitTable, string conduitType, DataTable elementsAdded)
        {

            double areaOcupada = 0.0;

            int qtCabos = 0;

            foreach (DataRow lineFromTable in elementsAdded.Rows)
            {

                if (lineFromTable["Triplex"].ToString() == "True")
                {

                    var distinctCable = cableTable.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == lineFromTable["Level"].ToString() &&
                                                 row.Field<string>("Type") == lineFromTable["Type"].ToString() &&
                                                 row.Field<string>("Conductors") == lineFromTable["Conductors"].ToString() &&
                                                 row.Field<string>("Size") == lineFromTable["Size"].ToString() &&
                                                 row.Field<string>("QtConductors") == lineFromTable["QtConductors"].ToString())

                                    .Select(row => row.Field<string>("OD")).FirstOrDefault();




                    double areaCable = Math.PI * Math.Pow((Convert.ToDouble(distinctCable) / 2), 2);


                    var groundCable = cableTable.AsEnumerable()
                        .Where(row => row.Field<string>("Level") == "GND" &&
                                row.Field<string>("Size") == lineFromTable["Ground"].ToString())
                        .Select(row => row.Field<string>("OD")).FirstOrDefault();

                    double areaGround = Math.PI * Math.Pow((Convert.ToDouble(groundCable) / 2), 2);


                    areaOcupada = areaOcupada + areaCable * 3 + areaGround;

                    qtCabos = qtCabos + 4;

                }

                else
                {

                    var distinctCable = cableTable.AsEnumerable()
                                   .Where(row => row.Field<string>("Level") == lineFromTable["Level"].ToString() &&
                                                row.Field<string>("Type") == lineFromTable["Type"].ToString() &&
                                                row.Field<string>("Conductors") == lineFromTable["Conductors"].ToString() &&
                                                row.Field<string>("Size") == lineFromTable["Size"].ToString() &&
                                                row.Field<string>("QtConductors") == lineFromTable["QtConductors"].ToString())

                                   .Select(row => row.Field<string>("OD")).FirstOrDefault();

                    double areaCable = Math.PI * Math.Pow((Convert.ToDouble(distinctCable) / 2), 2);

                    areaOcupada = areaOcupada + Convert.ToDouble(areaCable);

                    qtCabos = qtCabos + 1;
                }
            }



            DataTable conduitTableFiltered = conduitTable.AsEnumerable()
                    .Where(row => row.Field<string>("Type") == conduitType)
                    .CopyToDataTable();


            string selectedConduit = "";

            double factor = 0;

            switch (qtCabos)
            {
                case 1:
                    factor = 0.53;
                    break;
                case 2:
                    factor = 0.31;
                    break;
                default:
                    factor = 0.40;
                    break;
            }

            foreach (DataRow row in conduitTableFiltered.Rows)
            {
                if (Convert.ToDouble(row["areaIn"]) * factor >= areaOcupada)
                {
                    selectedConduit = row["Size"].ToString();
                    break;
                }
            }

            if (selectedConduit == "")
                selectedConduit = "Overflow";



            return (selectedConduit, "b");
        }


    }
}
