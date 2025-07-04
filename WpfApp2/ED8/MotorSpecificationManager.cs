using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MotorSpecifications
{
    // Classe modelo para representar uma especificação de motor
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

    // Classe modelo para o arquivo JSON completo
    public class MotorSpecificationsData
    {
        public List<MotorSpecification> motor_specifications { get; set; }
        public object metadata { get; set; }
    }

    // Classe principal para gerenciar as especificações de motor
    public class MotorSpecificationManager
    {
        private List<MotorSpecification> _motorSpecifications;
        private readonly string _jsonFilePath;

        public MotorSpecificationManager()
        {
            // Caminho para o arquivo JSON na pasta ED8
            _jsonFilePath = Path.Combine("ED8", "motor_specifications.json");
            LoadSpecifications();
        }

        /// <summary>
        /// Carrega as especificações do motor a partir do arquivo JSON
        /// </summary>
        private void LoadSpecifications()
        {
            try
            {
                if (!File.Exists(_jsonFilePath))
                {
                    throw new FileNotFoundException($"Arquivo não encontrado: {_jsonFilePath}");
                }

                string jsonContent = File.ReadAllText(_jsonFilePath);
                var data = JsonSerializer.Deserialize<MotorSpecificationsData>(jsonContent);
                
                if (data?.motor_specifications == null)
                {
                    throw new InvalidOperationException("Dados de especificações de motor não encontrados no arquivo JSON");
                }

                _motorSpecifications = data.motor_specifications;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar especificações do motor: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca as especificações do motor baseado no HP e retorna uma lista com os valores solicitados
        /// </summary>
        /// <param name="motorHp">Potência do motor em HP (ex: "1/2", "3/4", "1", "1 1/2", etc.)</param>
        /// <returns>List<string> contendo: motor_curr_x, wire_size_phase, conduit_size_pvc, conduit_size_r_metal</returns>
        public List<string> GetMotorSpecifications(string motorHp)
        {
            if (string.IsNullOrWhiteSpace(motorHp))
            {
                throw new ArgumentException("HP do motor não pode ser nulo ou vazio", nameof(motorHp));
            }

            if (_motorSpecifications == null || !_motorSpecifications.Any())
            {
                throw new InvalidOperationException("Especificações do motor não foram carregadas");
            }

            // Busca a especificação correspondente ao HP fornecido
            var specification = _motorSpecifications.FirstOrDefault(spec => 
                string.Equals(spec.motor_hp, motorHp, StringComparison.OrdinalIgnoreCase));

            if (specification == null)
            {
                throw new ArgumentException($"Especificação não encontrada para motor HP: {motorHp}");
            }

            // Retorna a lista com os valores solicitados
            return new List<string>
            {
                specification.motor_curr_x.ToString(),
                specification.wire_size_phase,
                specification.conduit_size_pvc,
                specification.conduit_size_r_metal
            };
        }

        /// <summary>
        /// Retorna todos os valores de HP disponíveis
        /// </summary>
        /// <returns>Lista com todos os valores de HP disponíveis</returns>
        public List<string> GetAvailableMotorHpValues()
        {
            if (_motorSpecifications == null || !_motorSpecifications.Any())
            {
                return new List<string>();
            }

            return _motorSpecifications.Select(spec => spec.motor_hp).ToList();
        }

        /// <summary>
        /// Verifica se um valor de HP específico existe nas especificações
        /// </summary>
        /// <param name="motorHp">HP do motor para verificar</param>
        /// <returns>True se o HP existe, False caso contrário</returns>
        public bool IsMotorHpAvailable(string motorHp)
        {
            if (string.IsNullOrWhiteSpace(motorHp) || _motorSpecifications == null)
            {
                return false;
            }

            return _motorSpecifications.Any(spec => 
                string.Equals(spec.motor_hp, motorHp, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retorna a especificação completa do motor baseado no HP
        /// </summary>
        /// <param name="motorHp">HP do motor</param>
        /// <returns>Objeto MotorSpecification completo ou null se não encontrado</returns>
        public MotorSpecification GetCompleteMotorSpecification(string motorHp)
        {
            if (string.IsNullOrWhiteSpace(motorHp) || _motorSpecifications == null)
            {
                return null;
            }

            return _motorSpecifications.FirstOrDefault(spec => 
                string.Equals(spec.motor_hp, motorHp, StringComparison.OrdinalIgnoreCase));
        }
    }
}

