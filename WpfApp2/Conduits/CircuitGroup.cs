using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp2.Conduits
{
    /// <summary>
    /// Representa um grupo de circuitos que compartilham o mesmo conduit
    /// </summary>
    public class CircuitGroup
    {
        public string ConduitTag { get; set; }
        public List<CircuitData> Circuits { get; set; }
        public double TotalOccupiedArea { get; set; }
        public int TotalCableCount { get; set; }
        public string SizedConduit { get; set; }
        public double MinimumRequiredArea { get; set; }
        public double FillFactor { get; set; }
        public string ConduitType { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public CircuitGroup(string tag)
        {
            ConduitTag = tag ?? throw new ArgumentNullException(nameof(tag));
            Circuits = new List<CircuitData>();
            TotalOccupiedArea = 0.0;
            TotalCableCount = 0;
            SizedConduit = string.Empty;
            MinimumRequiredArea = 0.0;
            FillFactor = 0.0;
            ConduitType = string.Empty;
            HasError = false;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Adiciona um circuito ao grupo
        /// </summary>
        public void AddCircuit(CircuitData circuit)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));

            if (circuit.ConduitTag != ConduitTag)
                throw new ArgumentException($"Circuit conduit tag '{circuit.ConduitTag}' does not match group tag '{ConduitTag}'");

            Circuits.Add(circuit);
        }

        /// <summary>
        /// Retorna uma descrição dos circuitos incluídos no grupo
        /// </summary>
        public string GetCircuitsDescription()
        {
            if (Circuits.Count == 0)
                return "Nenhum circuito";

            if (Circuits.Count == 1)
                return $"Circuito {Circuits[0].Numb}";

            var numbers = Circuits.Select(c => c.Numb).OrderBy(n => n);
            return $"Circuitos {string.Join(", ", numbers)}";
        }

        /// <summary>
        /// Retorna uma descrição detalhada dos tipos de circuitos
        /// </summary>
        public string GetDetailedDescription()
        {
            if (Circuits.Count == 0)
                return "Grupo vazio";

            var descriptions = new List<string>();
            
            foreach (var circuit in Circuits.OrderBy(c => c.Numb))
            {
                string desc = $"#{circuit.Numb}: {circuit.Level} {circuit.Type} {circuit.Conductors}";
                if (circuit.Conductors == "Multiconductor")
                {
                    desc += $" {circuit.QtConductors}x{circuit.Size}";
                }
                else
                {
                    desc += $" {circuit.Size}";
                    if (circuit.Triplex)
                    {
                        desc += $" (Triplex + {circuit.Ground} GND)";
                    }
                }
                descriptions.Add(desc);
            }

            return string.Join("; ", descriptions);
        }

        /// <summary>
        /// Valida se o grupo tem dados consistentes
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(ConduitTag))
                return false;

            if (Circuits.Count == 0)
                return false;

            // Verificar se todos os circuitos têm o mesmo conduit tag
            return Circuits.All(c => c.ConduitTag == ConduitTag);
        }

        /// <summary>
        /// Define um erro para o grupo
        /// </summary>
        public void SetError(string errorMessage)
        {
            HasError = true;
            ErrorMessage = errorMessage ?? string.Empty;
            SizedConduit = "ERRO";
        }

        /// <summary>
        /// Limpa qualquer erro definido para o grupo
        /// </summary>
        public void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}

