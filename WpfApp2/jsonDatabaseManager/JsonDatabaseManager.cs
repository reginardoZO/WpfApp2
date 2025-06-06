using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonDatabaseManager
{
    /// <summary>
    /// Classe responsável por gerenciar consultas em arquivos JSON como se fossem tabelas de banco de dados.
    /// </summary>
    public class JsonDbManager
    {
        private readonly string _databasePath;
        private Dictionary<string, List<Dictionary<string, object>>> _tables;

        /// <summary>
        /// Inicializa uma nova instância da classe JsonDbManager.
        /// </summary>
        /// <param name="databasePath">Caminho para a pasta contendo os arquivos JSON.</param>
        public JsonDbManager(string databasePath)
        {
            _databasePath = databasePath;
            _tables = new Dictionary<string, List<Dictionary<string, object>>>();
            LoadAllTables();
        }

        /// <summary>
        /// Carrega todos os arquivos JSON da pasta de banco de dados.
        /// </summary>
        private void LoadAllTables()
        {
            _tables.Clear();
            
            string[] jsonFiles = Directory.GetFiles(_databasePath, "*.json");
            foreach (string file in jsonFiles)
            {
                string tableName = Path.GetFileNameWithoutExtension(file);
                LoadTable(tableName);
            }
        }

        /// <summary>
        /// Carrega uma tabela específica a partir de um arquivo JSON.
        /// </summary>
        /// <param name="tableName">Nome da tabela (sem extensão .json).</param>
        public void LoadTable(string tableName)
        {
            string filePath = Path.Combine(_databasePath, $"{tableName}.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Arquivo JSON não encontrado: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent, options);
            _tables[tableName.ToLower()] = data;
        }

        /// <summary>
        /// Executa uma consulta SQL simples nos dados JSON carregados.
        /// </summary>
        /// <param name="sqlQuery">Consulta SQL a ser executada.</param>
        /// <returns>DataTable contendo os resultados da consulta.</returns>
        public DataTable ExecuteQuery(string sqlQuery)
        {
            // Analisar a consulta SQL
            var queryInfo = ParseSqlQuery(sqlQuery);
            
            // Verificar se a tabela existe
            if (!_tables.ContainsKey(queryInfo.TableName.ToLower()))
            {
                throw new Exception($"Tabela '{queryInfo.TableName}' não encontrada.");
            }

            // Obter os dados da tabela
            var tableData = _tables[queryInfo.TableName.ToLower()];
            
            // Aplicar filtros WHERE
            var filteredData = ApplyWhereConditions(tableData, queryInfo.WhereConditions);
            
            // Selecionar colunas
            var result = SelectColumns(filteredData, queryInfo.Columns);
            
            // Converter para DataTable
            return ConvertToDataTable(result, queryInfo.Columns);
        }

        /// <summary>
        /// Analisa uma consulta SQL simples.
        /// </summary>
        /// <param name="sqlQuery">Consulta SQL a ser analisada.</param>
        /// <returns>Informações da consulta.</returns>
        private QueryInfo ParseSqlQuery(string sqlQuery)
        {
            var queryInfo = new QueryInfo();
            
            // Regex para extrair partes da consulta SELECT
            var selectRegex = new Regex(@"SELECT\s+(.*?)\s+FROM\s+(\w+)(?:\s+WHERE\s+(.*))?", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            var match = selectRegex.Match(sqlQuery);
            if (!match.Success)
            {
                throw new ArgumentException("Formato de consulta SQL inválido. Use: SELECT colunas FROM tabela [WHERE condições]");
            }
            
            // Extrair colunas
            string columnsStr = match.Groups[1].Value.Trim();
            if (columnsStr == "*")
            {
                queryInfo.Columns = null; // Todas as colunas
            }
            else
            {
                queryInfo.Columns = columnsStr.Split(',')
                    .Select(c => c.Trim())
                    .ToList();
            }
            
            // Extrair nome da tabela
            queryInfo.TableName = match.Groups[2].Value.Trim();
            
            // Extrair condições WHERE, se existirem
            if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
            {
                string whereStr = match.Groups[3].Value.Trim();
                queryInfo.WhereConditions = ParseWhereConditions(whereStr);
            }
            
            return queryInfo;
        }

        /// <summary>
        /// Analisa as condições WHERE de uma consulta SQL.
        /// </summary>
        /// <param name="whereStr">String contendo as condições WHERE.</param>
        /// <returns>Lista de condições WHERE.</returns>
        private List<WhereCondition> ParseWhereConditions(string whereStr)
        {
            var conditions = new List<WhereCondition>();
            
            // Dividir por AND (implementação simples, não suporta OR ou parênteses)
            var andConditions = whereStr.Split(new[] { " AND " }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var condition in andConditions)
            {
                // Procurar por operadores de comparação
                foreach (var op in new[] { "=", "<>", "!=", ">", "<", ">=", "<=", "LIKE" })
                {
                    if (condition.Contains(op))
                    {
                        var parts = condition.Split(new[] { op }, 2, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            string column = parts[0].Trim();
                            string value = parts[1].Trim();
                            
                            // Remover aspas se existirem
                            if ((value.StartsWith("'") && value.EndsWith("'")) || 
                                (value.StartsWith("\"") && value.EndsWith("\"")))
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            
                            conditions.Add(new WhereCondition
                            {
                                Column = column,
                                Operator = op,
                                Value = value
                            });
                            break;
                        }
                    }
                }
            }
            
            return conditions;
        }

        /// <summary>
        /// Aplica as condições WHERE aos dados.
        /// </summary>
        /// <param name="data">Dados a serem filtrados.</param>
        /// <param name="conditions">Condições WHERE a serem aplicadas.</param>
        /// <returns>Dados filtrados.</returns>
        private List<Dictionary<string, object>> ApplyWhereConditions(
            List<Dictionary<string, object>> data, 
            List<WhereCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return data;
            }
            
            return data.Where(row => 
            {
                bool match = true;
                foreach (var condition in conditions)
                {
                    if (!row.ContainsKey(condition.Column))
                    {
                        // Tentar com case insensitive
                        var key = row.Keys.FirstOrDefault(k => 
                            string.Equals(k, condition.Column, StringComparison.OrdinalIgnoreCase));
                        
                        if (key == null)
                        {
                            match = false;
                            break;
                        }
                        
                        condition.Column = key;
                    }
                    
                    var rowValue = row[condition.Column]?.ToString();
                    
                    switch (condition.Operator.ToUpper())
                    {
                        case "=":
                            match = string.Equals(rowValue, condition.Value, 
                                StringComparison.OrdinalIgnoreCase);
                            break;
                        case "<>":
                        case "!=":
                            match = !string.Equals(rowValue, condition.Value, 
                                StringComparison.OrdinalIgnoreCase);
                            break;
                        case ">":
                            match = CompareValues(rowValue, condition.Value) > 0;
                            break;
                        case "<":
                            match = CompareValues(rowValue, condition.Value) < 0;
                            break;
                        case ">=":
                            match = CompareValues(rowValue, condition.Value) >= 0;
                            break;
                        case "<=":
                            match = CompareValues(rowValue, condition.Value) <= 0;
                            break;
                        case "LIKE":
                            var pattern = "^" + Regex.Escape(condition.Value)
                                .Replace("%", ".*")
                                .Replace("_", ".") + "$";
                            match = Regex.IsMatch(rowValue ?? "", pattern, RegexOptions.IgnoreCase);
                            break;
                    }
                    
                    if (!match)
                    {
                        break;
                    }
                }
                
                return match;
            }).ToList();
        }

        /// <summary>
        /// Compara dois valores como strings ou números.
        /// </summary>
        /// <param name="value1">Primeiro valor.</param>
        /// <param name="value2">Segundo valor.</param>
        /// <returns>Resultado da comparação.</returns>
        private int CompareValues(string value1, string value2)
        {
            // Tentar comparar como números
            if (double.TryParse(value1, out double num1) && double.TryParse(value2, out double num2))
            {
                return num1.CompareTo(num2);
            }
            
            // Comparar como strings
            return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Seleciona colunas específicas dos dados.
        /// </summary>
        /// <param name="data">Dados a serem processados.</param>
        /// <param name="columns">Colunas a serem selecionadas.</param>
        /// <returns>Dados com apenas as colunas selecionadas.</returns>
        private List<Dictionary<string, object>> SelectColumns(
            List<Dictionary<string, object>> data, 
            List<string> columns)
        {
            if (columns == null || columns.Count == 0)
            {
                return data;
            }
            
            var result = new List<Dictionary<string, object>>();
            
            foreach (var row in data)
            {
                var newRow = new Dictionary<string, object>();
                
                foreach (var column in columns)
                {
                    // Procurar coluna com case insensitive
                    var key = row.Keys.FirstOrDefault(k => 
                        string.Equals(k, column, StringComparison.OrdinalIgnoreCase));
                    
                    if (key != null)
                    {
                        newRow[key] = row[key];
                    }
                }
                
                result.Add(newRow);
            }
            
            return result;
        }

        /// <summary>
        /// Converte uma lista de dicionários para um DataTable.
        /// </summary>
        /// <param name="data">Dados a serem convertidos.</param>
        /// <param name="columns">Colunas a serem incluídas.</param>
        /// <returns>DataTable contendo os dados.</returns>
        private DataTable ConvertToDataTable(
     List<Dictionary<string, object>> data,
     List<string> columns)
        {
            var table = new DataTable();

            if (data.Count == 0)
            {
                return table;
            }

            // Determinar as colunas
            HashSet<string> columnSet = new HashSet<string>();

            if (columns != null && columns.Count > 0)
            {
                // Usar as colunas especificadas
                foreach (var column in columns)
                {
                    columnSet.Add(column);
                }
            }
            else
            {
                // Usar todas as colunas do primeiro registro
                foreach (var key in data[0].Keys)
                {
                    columnSet.Add(key);
                }
            }

            // Adicionar colunas ao DataTable
            foreach (var column in columnSet)
            {
                table.Columns.Add(column, typeof(string));
            }

            // Adicionar linhas ao DataTable
            foreach (var row in data)
            {
                var dataRow = table.NewRow();

                foreach (var column in columnSet)
                {
                    // Procurar coluna com case insensitive
                    var key = row.Keys.FirstOrDefault(k =>
                        string.Equals(k, column, StringComparison.OrdinalIgnoreCase));

                    if (key != null)
                    {
                        // Versão corrigida: usando verificação explícita para null
                        object value = row[key];
                        dataRow[column] = value == null ? DBNull.Value : value.ToString();
                    }
                    else
                    {
                        dataRow[column] = DBNull.Value;
                    }
                }

                table.Rows.Add(dataRow);
            }

            return table;
        }

        /// <summary>
        /// Classe para armazenar informações de uma consulta SQL.
        /// </summary>
        private class QueryInfo
        {
            public string TableName { get; set; }
            public List<string> Columns { get; set; }
            public List<WhereCondition> WhereConditions { get; set; } = new List<WhereCondition>();
        }

        /// <summary>
        /// Classe para armazenar uma condição WHERE.
        /// </summary>
        private class WhereCondition
        {
            public string Column { get; set; }
            public string Operator { get; set; }
            public string Value { get; set; }
        }
    }
}

