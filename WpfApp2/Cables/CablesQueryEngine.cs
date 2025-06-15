using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace CablesQuery
{
    /// <summary>
    /// Classe para realizar consultas SQL-like em dados de cabos armazenados em JSON
    /// </summary>
    public class CablesQueryEngine
    {
        private Dictionary<string, List<Dictionary<string, object>>> _data;
        private string _jsonFilePath;

        /// <summary>
        /// Construtor que carrega os dados do arquivo JSON
        /// </summary>
        /// <param name="jsonFilePath">Caminho para o arquivo cablesSpecs.json</param>
        /// 

        public CablesQueryEngine(string jsonFilePath = null)
        {
            if (string.IsNullOrEmpty(jsonFilePath))
            {
                _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cables", "cablesSpecs.json");
            }
            else
            {
                _jsonFilePath = jsonFilePath;
            }

            LoadData();
        }

        /// <summary>
        /// Carrega os dados do arquivo JSON
        /// </summary>
        private void LoadData()
        {
            try
            {
                if (!File.Exists(_jsonFilePath))
                {
                    throw new FileNotFoundException($"Arquivo JSON não encontrado: {_jsonFilePath}");
                }

                string jsonContent = File.ReadAllText(_jsonFilePath);
                var jsonDocument = JsonDocument.Parse(jsonContent);
                
                _data = new Dictionary<string, List<Dictionary<string, object>>>();

                foreach (var property in jsonDocument.RootElement.EnumerateObject())
                {
                    var tableName = property.Name;
                    var records = new List<Dictionary<string, object>>();

                    foreach (var record in property.Value.EnumerateArray())
                    {
                        var recordDict = new Dictionary<string, object>();
                        
                        foreach (var field in record.EnumerateObject())
                        {
                            object value = field.Value.ValueKind switch
                            {
                                JsonValueKind.String => field.Value.GetString(),
                                JsonValueKind.Number => field.Value.TryGetInt32(out int intVal) ? intVal : field.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => null,
                                _ => field.Value.ToString()
                            };
                            
                            recordDict[field.Name] = value;
                        }
                        
                        records.Add(recordDict);
                    }
                    
                    _data[tableName] = records;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar dados do JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executa uma consulta SQL-like e retorna um DataTable
        /// </summary>
        /// <param name="query">Consulta SQL (ex: "SELECT size, od, weight FROM multi WHERE amp90 > 300")</param>
        /// <returns>DataTable com os resultados da consulta</returns>
        public DataTable ExecuteQuery(string query)
        {
            try
            {
                // Normalizar a query (remover espaços extras e converter para minúsculas para análise)
                string normalizedQuery = Regex.Replace(query.Trim(), @"\s+", " ");
                
                // Parse da query
                var queryParts = ParseQuery(normalizedQuery);
                
                // Obter dados da tabela
                if (!_data.ContainsKey(queryParts.TableName))
                {
                    throw new ArgumentException($"Tabela '{queryParts.TableName}' não encontrada. Tabelas disponíveis: {string.Join(", ", _data.Keys)}");
                }

                var tableData = _data[queryParts.TableName];
                
                // Aplicar filtros WHERE
                var filteredData = ApplyWhereClause(tableData, queryParts.WhereClause);
                
                // Criar DataTable com colunas selecionadas
                return CreateDataTable(filteredData, queryParts.SelectColumns);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao executar consulta: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Estrutura para armazenar partes da query parseada
        /// </summary>
        private class QueryParts
        {
            public List<string> SelectColumns { get; set; } = new List<string>();
            public string TableName { get; set; }
            public string WhereClause { get; set; }
        }

        /// <summary>
        /// Faz o parse da query SQL
        /// </summary>
        private QueryParts ParseQuery(string query)
        {
            var parts = new QueryParts();
            
            // Regex para capturar SELECT ... FROM ... WHERE ...
            var selectRegex = new Regex(@"SELECT\s+(.*?)\s+FROM\s+(\w+)(?:\s+WHERE\s+(.*))?", RegexOptions.IgnoreCase);
            var match = selectRegex.Match(query);
            
            if (!match.Success)
            {
                throw new ArgumentException("Formato de query inválido. Use: SELECT campos FROM tabela [WHERE condições]");
            }
            
            // Colunas SELECT
            string selectPart = match.Groups[1].Value.Trim();
            if (selectPart == "*")
            {
                parts.SelectColumns.Add("*");
            }
            else
            {
                parts.SelectColumns = selectPart.Split(',')
                    .Select(col => col.Trim())
                    .ToList();
            }
            
            // Nome da tabela
            parts.TableName = match.Groups[2].Value.Trim();
            
            // Cláusula WHERE (opcional)
            if (match.Groups[3].Success)
            {
                parts.WhereClause = match.Groups[3].Value.Trim();
            }
            
            return parts;
        }

        /// <summary>
        /// Aplica filtros da cláusula WHERE
        /// </summary>
        private List<Dictionary<string, object>> ApplyWhereClause(List<Dictionary<string, object>> data, string whereClause)
        {
            if (string.IsNullOrEmpty(whereClause))
            {
                return data;
            }

            return data.Where(record => EvaluateWhereCondition(record, whereClause)).ToList();
        }

        /// <summary>
        /// Avalia uma condição WHERE para um registro
        /// </summary>
        private bool EvaluateWhereCondition(Dictionary<string, object> record, string condition)
        {
            // Suporte para operadores básicos: =, >, <, >=, <=, !=
            var operatorRegex = new Regex(@"(\w+)\s*(>=|<=|!=|>|<|=)\s*(.+)", RegexOptions.IgnoreCase);
            var match = operatorRegex.Match(condition);
            
            if (!match.Success)
            {
                return true; // Se não conseguir parsear, retorna true
            }
            
            string fieldName = match.Groups[1].Value.Trim();
            string operatorSymbol = match.Groups[2].Value.Trim();
            string valueStr = match.Groups[3].Value.Trim().Trim('\'', '"');
            
            if (!record.ContainsKey(fieldName))
            {
                return false;
            }
            
            object fieldValue = record[fieldName];
            
            // Tentar converter valores para comparação
            if (double.TryParse(valueStr, out double numericValue) && 
                fieldValue != null && 
                (fieldValue is int || fieldValue is double || fieldValue is decimal))
            {
                double fieldNumericValue = Convert.ToDouble(fieldValue);
                
                return operatorSymbol switch
                {
                    "=" => Math.Abs(fieldNumericValue - numericValue) < 0.0001,
                    ">" => fieldNumericValue > numericValue,
                    "<" => fieldNumericValue < numericValue,
                    ">=" => fieldNumericValue >= numericValue,
                    "<=" => fieldNumericValue <= numericValue,
                    "!=" => Math.Abs(fieldNumericValue - numericValue) >= 0.0001,
                    _ => false
                };
            }
            else
            {
                // Comparação de strings
                string fieldStringValue = fieldValue?.ToString() ?? "";
                
                return operatorSymbol switch
                {
                    "=" => fieldStringValue.Equals(valueStr, StringComparison.OrdinalIgnoreCase),
                    "!=" => !fieldStringValue.Equals(valueStr, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }
        }

        /// <summary>
        /// Cria um DataTable com os dados filtrados e colunas selecionadas
        /// </summary>
        private DataTable CreateDataTable(List<Dictionary<string, object>> data, List<string> selectColumns)
        {
            var dataTable = new DataTable();
            
            if (data.Count == 0)
            {
                return dataTable;
            }
            
            // Determinar colunas a incluir
            var columnsToInclude = new List<string>();
            if (selectColumns.Contains("*"))
            {
                columnsToInclude = data[0].Keys.ToList();
            }
            else
            {
                columnsToInclude = selectColumns;
            }
            
            // Criar colunas no DataTable
            foreach (string columnName in columnsToInclude)
            {
                if (data[0].ContainsKey(columnName))
                {
                    var sampleValue = data[0][columnName];
                    Type columnType = sampleValue?.GetType() ?? typeof(string);
                    dataTable.Columns.Add(columnName, columnType);
                }
            }
            
            // Adicionar linhas
            foreach (var record in data)
            {
                var row = dataTable.NewRow();
                foreach (string columnName in columnsToInclude)
                {
                    if (record.ContainsKey(columnName) && dataTable.Columns.Contains(columnName))
                    {
                        row[columnName] = record[columnName] ?? DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }
            
            return dataTable;
        }

        /// <summary>
        /// Retorna lista das tabelas disponíveis
        /// </summary>
        public List<string> GetAvailableTables()
        {
            return _data.Keys.ToList();
        }

        /// <summary>
        /// Retorna lista das colunas de uma tabela específica
        /// </summary>
        public List<string> GetTableColumns(string tableName)
        {
            if (!_data.ContainsKey(tableName) || _data[tableName].Count == 0)
            {
                return new List<string>();
            }
            
            return _data[tableName][0].Keys.ToList();
        }

        /// <summary>
        /// Recarrega os dados do arquivo JSON
        /// </summary>
        public void RefreshData()
        {
            LoadData();
        }
    }
}

