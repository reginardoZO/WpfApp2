using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using OfficeOpenXml;

namespace WpfApp2.Conduits
{
    /// <summary>
    /// Processa o dimensionamento de conduits em lote a partir de um arquivo Excel.
    /// VERSÃO CORRIGIDA - Fix para Total Cable Count
    /// </summary>
    public class BatchProcessor
    {
        private readonly DataTable cableTable;
        private readonly DataTable conduitTable;
        private readonly calc calculator;

        public BatchProcessor(DataTable cables, DataTable conduits)
        {
            cableTable = cables ?? throw new ArgumentNullException(nameof(cables));
            conduitTable = conduits ?? throw new ArgumentNullException(nameof(conduits));
            calculator = new calc();
        }

        /// <summary>
        /// Processa o arquivo Excel de entrada e retorna o caminho do arquivo de saída.
        /// </summary>
        public string ProcessBatch(string inputFilePath, string selectedConduitType)
        {
            if (string.IsNullOrEmpty(inputFilePath))
                throw new ArgumentException("Caminho do arquivo de entrada não pode ser vazio.", nameof(inputFilePath));
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", inputFilePath);
            if (string.IsNullOrEmpty(selectedConduitType))
                throw new ArgumentException("Tipo de conduit deve ser selecionado.", nameof(selectedConduitType));

            // 1. Ler dados do Excel
            DataTable inputData = ReadExcelData(inputFilePath);

            // 2. Agrupar circuitos por Conduit Tag
            Dictionary<string, CircuitGroup> circuitGroups = GroupCircuits(inputData);

            // 3. Calcular dimensionamento para cada grupo
            foreach (var group in circuitGroups.Values)
            {
                CalculateGroupSizing(group, selectedConduitType);
            }

            // 4. Gerar arquivo Excel de saída
            string outputFilePath = GenerateOutputFilePath(inputFilePath);
            ExportResultsToExcel(inputFilePath, outputFilePath, circuitGroups.Values.ToList());

            return outputFilePath;
        }

        /// <summary>
        /// Lê os dados da planilha Excel para um DataTable.
        /// </summary>
        private DataTable ReadExcelData(string filePath)
        {
            DataTable dataTable = CreateCircuitsDataTable();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    throw new InvalidOperationException("O arquivo Excel não contém planilhas.");
                }

                int rowCount = worksheet.Dimension?.Rows ?? 0;
                int colCount = worksheet.Dimension?.Columns ?? 0;

                if (rowCount <= 1)
                {
                    throw new InvalidOperationException("O arquivo Excel não contém dados válidos (apenas cabeçalho ou vazio).");
                }

                if (colCount < 9)
                {
                    throw new InvalidOperationException($"A planilha deve ter pelo menos 9 colunas. Encontradas: {colCount}");
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    DataRow dataRow = dataTable.NewRow();
                    try
                    {
                        dataRow["Numb"] = GetCellValue(worksheet, row, 1, 0);
                        dataRow["Level"] = GetCellValue(worksheet, row, 2, string.Empty);
                        dataRow["Type"] = GetCellValue(worksheet, row, 3, string.Empty);
                        dataRow["Conductors"] = GetCellValue(worksheet, row, 4, string.Empty);
                        dataRow["Size"] = GetCellValue(worksheet, row, 5, string.Empty);
                        dataRow["QtConductors"] = GetCellValue(worksheet, row, 6, 0);
                        dataRow["Ground"] = GetCellValue<string>(worksheet, row, 7, null);
                        dataRow["Triplex"] = GetCellValue<object>(worksheet, row, 8, null);
                        dataRow["Conduit"] = GetCellValue(worksheet, row, 9, string.Empty);

                        if (string.IsNullOrEmpty(dataRow["Conduit"]?.ToString()))
                        {
                            throw new DataException($"Coluna 'Conduit' está vazia ou ausente na linha {row}.");
                        }

                        dataTable.Rows.Add(dataRow);
                    }
                    catch (Exception ex)
                    {
                        throw new DataException($"Erro ao ler a linha {row}: {ex.Message}", ex);
                    }
                }
            }
            return dataTable;
        }

        /// <summary>
        /// Agrupa os circuitos lidos do DataTable por Conduit Tag.
        /// </summary>
        private Dictionary<string, CircuitGroup> GroupCircuits(DataTable inputData)
        {
            var groups = new Dictionary<string, CircuitGroup>();

            foreach (DataRow row in inputData.Rows)
            {
                string conduitTag = row["Conduit"]?.ToString()?.Trim();

                if (string.IsNullOrEmpty(conduitTag))
                {
                    Console.WriteLine($"Aviso: Linha {row["Numb"]} ignorada por não ter Conduit Tag.");
                    continue;
                }

                if (!groups.ContainsKey(conduitTag))
                {
                    groups[conduitTag] = new CircuitGroup(conduitTag);
                }

                try
                {
                    CircuitData circuit = new CircuitData(row);
                    groups[conduitTag].AddCircuit(circuit);
                }
                catch (Exception ex)
                {
                    if (!groups.ContainsKey(conduitTag))
                    {
                        groups[conduitTag] = new CircuitGroup(conduitTag);
                    }
                    groups[conduitTag].SetError($"Erro ao processar circuito Numb {row["Numb"]}: {ex.Message}");
                    Console.WriteLine($"Erro ao processar circuito Numb {row["Numb"]} para o grupo {conduitTag}: {ex.Message}");
                }
            }

            return groups;
        }

        /// <summary>
        /// Calcula o dimensionamento para um grupo de circuitos.
        /// VERSÃO CORRIGIDA - Calcula contagem de cabos diretamente
        /// </summary>
        private void CalculateGroupSizing(CircuitGroup group, string selectedConduitType)
        {
            if (group == null || group.HasError)
            {
                return;
            }

            group.ConduitType = selectedConduitType;

            try
            {
                // CORREÇÃO: Calcular área ocupada e contagem de cabos ANTES de chamar sizeConduit
                double totalOccupiedArea = 0.0;
                int totalCableCount = 0;

                foreach (var circuit in group.Circuits)
                {
                    var (area, cableCount) = CalculateCircuitContribution(circuit);
                    totalOccupiedArea += area;
                    totalCableCount += cableCount;
                }

                // Definir os valores calculados no grupo
                group.TotalOccupiedArea = totalOccupiedArea;
                group.TotalCableCount = totalCableCount;

                // Calcular fator de preenchimento baseado na contagem total
                double fillFactor = CalculateFillFactor(totalCableCount);
                group.FillFactor = fillFactor;

                // Calcular área mínima necessária
                group.MinimumRequiredArea = totalOccupiedArea / fillFactor;

                // Criar DataTable temporário para usar com a classe calc
                DataTable groupTable = CreateCircuitsDataTable();
                foreach (var circuit in group.Circuits)
                {
                    groupTable.Rows.Add(circuit.ToDataRow(groupTable));
                }

                // Chamar o método de cálculo existente para obter o conduit dimensionado
                (string sized, FlowDocument calculationDocument) result = calculator.sizeConduit(cableTable, conduitTable, selectedConduitType, groupTable);

                group.SizedConduit = result.sized;

                // Verificar se o cálculo falhou
                if (group.SizedConduit == "Error" || group.SizedConduit == "Overflow" || group.SizedConduit == "N/A")
                {
                    string errorMsg = GetLastErrorFromFlowDocument(result.calculationDocument);
                    group.SetError(string.IsNullOrEmpty(errorMsg) ? $"Falha no cálculo: {group.SizedConduit}" : errorMsg);
                }
            }
            catch (Exception ex)
            {
                group.SetError($"Erro inesperado durante o cálculo do grupo {group.ConduitTag}: {ex.Message}");
                Console.WriteLine($"Erro ao calcular grupo {group.ConduitTag}: {ex.Message}");
            }
        }

        /// <summary>
        /// NOVO MÉTODO: Calcula a contribuição de área e contagem de cabos de um circuito individual
        /// </summary>
        private (double area, int cableCount) CalculateCircuitContribution(CircuitData circuit)
        {
            double area = 0.0;
            int cableCount = 0;

            try
            {
                if (circuit.Triplex)
                {
                    // Para Triplex: 3 condutores de fase + 1 condutor de aterramento

                    // Buscar OD do cabo de fase
                    var phaseOD = cableTable.AsEnumerable()
                        .Where(row => row.Field<string>("Level") == circuit.Level &&
                                     row.Field<string>("Type") == circuit.Type &&
                                     row.Field<string>("Conductors") == circuit.Conductors &&
                                     row.Field<string>("Size") == circuit.Size &&
                                     row.Field<string>("QtConductors") == circuit.QtConductors.ToString())
                        .Select(row => row.Field<string>("OD"))
                        .FirstOrDefault();

                    if (phaseOD != null && double.TryParse(phaseOD, out double phaseODValue))
                    {
                        double phaseArea = Math.PI * Math.Pow(phaseODValue / 2, 2);
                        area += phaseArea * 3; // 3 condutores de fase
                        cableCount += 3;

                        // Buscar OD do cabo de aterramento
                        if (!string.IsNullOrEmpty(circuit.Ground))
                        {
                            var groundOD = cableTable.AsEnumerable()
                                .Where(row => row.Field<string>("Level") == "GND" &&
                                             row.Field<string>("Size") == circuit.Ground)
                                .Select(row => row.Field<string>("OD"))
                                .FirstOrDefault();

                            if (groundOD != null && double.TryParse(groundOD, out double groundODValue))
                            {
                                double groundArea = Math.PI * Math.Pow(groundODValue / 2, 2);
                                area += groundArea;
                                cableCount += 1;
                            }
                        }
                    }
                }
                else
                {
                    // Para não-Triplex: usar QtConductors
                    var cableOD = cableTable.AsEnumerable()
                        .Where(row => row.Field<string>("Level") == circuit.Level &&
                                     row.Field<string>("Type") == circuit.Type &&
                                     row.Field<string>("Conductors") == circuit.Conductors &&
                                     row.Field<string>("Size") == circuit.Size &&
                                     row.Field<string>("QtConductors") == circuit.QtConductors.ToString())
                        .Select(row => row.Field<string>("OD"))
                        .FirstOrDefault();

                    if (cableOD != null && double.TryParse(cableOD, out double cableODValue))
                    {
                        double cableArea = Math.PI * Math.Pow(cableODValue / 2, 2);

                        if (circuit.Conductors == "Multiconductor")
                        {
                            // Para multiconductor, é um cabo único
                            area += cableArea;
                            cableCount += 1;
                        }
                        else
                        {
                            // Para single, multiplicar pela quantidade
                            area += cableArea * circuit.QtConductors;
                            cableCount += circuit.QtConductors;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular contribuição do circuito {circuit.Numb}: {ex.Message}");
            }

            return (area, cableCount);
        }

        /// <summary>
        /// NOVO MÉTODO: Calcula o fator de preenchimento baseado na contagem de cabos
        /// </summary>
        private double CalculateFillFactor(int cableCount)
        {
            switch (cableCount)
            {
                case 0:
                    return 0.0;
                case 1:
                    return 0.53;
                case 2:
                    return 0.31;
                default:
                    return 0.40;
            }
        }

        /// <summary>
        /// Tenta extrair a última mensagem de erro do FlowDocument.
        /// </summary>
        private string GetLastErrorFromFlowDocument(FlowDocument doc)
        {
            if (doc == null) return string.Empty;
            try
            {
                string textContent = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
                var lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].Trim().StartsWith("ERROR:") || lines[i].Contains("No suitable conduit found") || lines[i].Contains("No cables added"))
                    {
                        return lines[i].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao extrair erro do FlowDocument: {ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>
        /// Gera o caminho para o arquivo de saída.
        /// </summary>
        private string GenerateOutputFilePath(string inputFilePath)
        {
            string directory = Path.GetDirectoryName(inputFilePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            string extension = Path.GetExtension(inputFilePath);
            return Path.Combine(directory, $"{fileNameWithoutExt}_Sized{extension}");
        }

        /// <summary>
        /// Exporta os resultados para uma nova aba no arquivo Excel.
        /// </summary>
        private void ExportResultsToExcel(string inputFilePath, string outputFilePath, List<CircuitGroup> results)
        {
            File.Copy(inputFilePath, outputFilePath, true);

            using (var package = new ExcelPackage(new FileInfo(outputFilePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("ConduitSizingResults");

                // Cabeçalhos
                worksheet.Cells[1, 1].Value = "Conduit Tag";
                worksheet.Cells[1, 2].Value = "Conduit Type";
                worksheet.Cells[1, 3].Value = "Circuits Included";
                worksheet.Cells[1, 4].Value = "Total Cable Count";
                worksheet.Cells[1, 5].Value = "Total Occupied Area (in²)";
                worksheet.Cells[1, 6].Value = "Fill Factor (%)";
                worksheet.Cells[1, 7].Value = "Min. Required Area (in²)";
                worksheet.Cells[1, 8].Value = "Sized Conduit";
                worksheet.Cells[1, 9].Value = "Status";
                worksheet.Cells[1, 10].Value = "Details / Error Message";

                // Formatar cabeçalhos
                using (var range = worksheet.Cells[1, 1, 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Preencher dados
                int row = 2;
                foreach (var group in results.OrderBy(g => g.ConduitTag))
                {
                    worksheet.Cells[row, 1].Value = group.ConduitTag;
                    worksheet.Cells[row, 2].Value = group.ConduitType;
                    worksheet.Cells[row, 3].Value = group.GetCircuitsDescription();
                    worksheet.Cells[row, 4].Value = group.TotalCableCount; // CORRIGIDO: Agora deve mostrar valor correto
                    worksheet.Cells[row, 5].Value = group.TotalOccupiedArea;
                    worksheet.Cells[row, 6].Value = group.FillFactor;
                    worksheet.Cells[row, 7].Value = group.MinimumRequiredArea;
                    worksheet.Cells[row, 8].Value = group.SizedConduit;
                    worksheet.Cells[row, 9].Value = group.HasError ? "Erro" : "OK";
                    worksheet.Cells[row, 10].Value = group.HasError ? group.ErrorMessage : group.GetDetailedDescription();

                    // Formatação de números
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "0.000000";
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "0.00%";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "0.000000";

                    // Destacar erros
                    if (group.HasError)
                    {
                        using (var range = worksheet.Cells[row, 1, row, 10])
                        {
                            range.Style.Font.Color.SetColor(System.Drawing.Color.Red);
                        }
                    }

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                package.Save();
            }
        }

        // Métodos auxiliares
        private DataTable CreateCircuitsDataTable()
        {
            DataTable dataTable = new DataTable("Circuits");
            dataTable.Columns.Add("Numb", typeof(int));
            dataTable.Columns.Add("Level", typeof(string));
            dataTable.Columns.Add("Type", typeof(string));
            dataTable.Columns.Add("Conductors", typeof(string));
            dataTable.Columns.Add("Size", typeof(string));
            dataTable.Columns.Add("QtConductors", typeof(int));
            dataTable.Columns.Add("Ground", typeof(string));
            dataTable.Columns.Add("Triplex", typeof(object));
            dataTable.Columns.Add("Conduit", typeof(string));
            dataTable.Columns["Ground"].AllowDBNull = true;
            dataTable.Columns["Triplex"].AllowDBNull = true;
            return dataTable;
        }

        private T GetCellValue<T>(ExcelWorksheet worksheet, int row, int col, T defaultValue)
        {
            try
            {
                var cellValue = worksheet.Cells[row, col].Value;

                if (cellValue == null || cellValue.ToString() == "")
                    return defaultValue;

                if (typeof(T) == typeof(bool))
                {
                    string lowerVal = cellValue.ToString().ToLower().Trim();
                    if (lowerVal == "true" || lowerVal == "1" || lowerVal == "yes" || lowerVal == "sim") return (T)(object)true;
                    if (lowerVal == "false" || lowerVal == "0" || lowerVal == "no" || lowerVal == "não") return (T)(object)false;
                    return defaultValue;
                }

                if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(cellValue.ToString(), out int intResult))
                        return (T)(object)intResult;
                    if (double.TryParse(cellValue.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double doubleResult))
                        return (T)(object)Convert.ToInt32(doubleResult);
                    return defaultValue;
                }

                if (typeof(T) == typeof(string))
                {
                    string stringValue = cellValue.ToString()?.Trim();
                    if (string.IsNullOrEmpty(stringValue))
                        return defaultValue;
                    return (T)(object)stringValue;
                }

                return (T)Convert.ChangeType(cellValue, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao converter célula [{row},{col}] para tipo {typeof(T).Name}: {ex.Message}. Valor: '{worksheet.Cells[row, col].Value}'. Usando valor padrão.");
                return defaultValue;
            }
        }
    }
}

