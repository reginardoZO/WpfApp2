using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WpfApp2.Conduits
{
    /// <summary>
    /// Representa os dados de um circuito individual
    /// </summary>
    public class CircuitData
    {
        public int Numb { get; set; }
        public string Level { get; set; }
        public string Type { get; set; }
        public string Conductors { get; set; }
        public string Size { get; set; }
        public int QtConductors { get; set; }
        public string Ground { get; set; }
        public bool Triplex { get; set; }
        public string ConduitTag { get; set; }

        public CircuitData() { }

        public CircuitData(DataRow row)
        {
            Numb = Convert.ToInt32(row["Numb"]);
            Level = row["Level"]?.ToString() ?? string.Empty;
            Type = row["Type"]?.ToString() ?? string.Empty;
            Conductors = row["Conductors"]?.ToString() ?? string.Empty;
            Size = row["Size"]?.ToString() ?? string.Empty;
            QtConductors = Convert.ToInt32(row["QtConductors"]);
            Ground = row["Ground"]?.ToString() ?? string.Empty;
            
            // Converter Triplex para bool
            if (row["Triplex"] != null && row["Triplex"] != DBNull.Value)
            {
                string triplexValue = row["Triplex"].ToString().ToLower();
                Triplex = triplexValue == "true" || triplexValue == "1" || triplexValue == "yes" || triplexValue == "sim";
            }
            else
            {
                Triplex = false;
            }
            
            ConduitTag = row["Conduit"]?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Converte o CircuitData para DataRow compatível com a classe calc existente
        /// </summary>
        public DataRow ToDataRow(DataTable table)
        {
            DataRow row = table.NewRow();
            row["Numb"] = Numb;
            row["Level"] = Level;
            row["Type"] = Type;
            row["Conductors"] = Conductors;
            row["Size"] = Size;
            row["QtConductors"] = QtConductors;
            row["Ground"] = Ground;
            row["Triplex"] = Triplex;
            return row;
        }
    }
}

