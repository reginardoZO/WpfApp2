using System.Data;
using System.Diagnostics;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Windows.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;
using System.Windows.Documents;
using OfficeOpenXml;
using Microsoft.Win32;
using System.IO;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for ConduitsView.xaml
    /// </summary>
    public partial class ConduitsView : UserControl
    {
        public DatabaseAccess acessos = new DatabaseAccess();
        DataTable dataCables = new DataTable();
        DataTable dataConduits = new DataTable();

        DataTable elementsAdded = new DataTable();


        calc classCalc = new calc();

        public ConduitsView()
        {
            InitializeComponent();

            //set datatable to add elements to grid

            elementsAdded.Rows.Clear();

            elementsAdded.Columns.Add("Numb", typeof(int));
            elementsAdded.Columns.Add("Level", typeof(string));
            elementsAdded.Columns.Add("Type", typeof(string));
            elementsAdded.Columns.Add("Conductors", typeof(string));
            elementsAdded.Columns.Add("Size", typeof(string));
            elementsAdded.Columns.Add("QtConductors", typeof(string));
            elementsAdded.Columns.Add("Ground", typeof(string));
            elementsAdded.Columns.Add("Triplex", typeof(bool));
            

            CircuitsDataGrid.ItemsSource = elementsAdded.DefaultView;


            List<string> conduitTypes = new List<string>();
            conduitTypes.Add("EMT - Electrical Metallic Tubing");
            conduitTypes.Add("ENT - Electrical Nonmetallic Tubing");
            conduitTypes.Add("FMC - Flexible Metal Conduit");
            conduitTypes.Add("IMC - Intermediate Metal Conduit");
            conduitTypes.Add("RMC - Rigid Metal Conduit");
            conduitTypes.Add("PVC - Rigid PVC Conduit - SCH 80");
            conduitTypes.Add("PVC/HDPE - Rigid PVC Conduit - SCH 40");

            cmbCondType.ItemsSource = conduitTypes;

            cmbCondType.SelectedIndex = 4;

            txtSizedConduit.Visibility = Visibility.Hidden;
            richConduit.Visibility = Visibility.Hidden;

        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // Show loading indicator
            LoadingIndicator.Visibility = Visibility.Visible;
            ContentGrid.IsEnabled = false;

            try
            {
                // Load data asynchronously to avoid blocking UI
                await Task.Run(() =>
                {
                    dataCables = acessos.ExecuteQuery("SELECT * FROM cables");
                    dataConduits = acessos.ExecuteQuery("SELECT * FROM conduitsSizes");
                });

                // Preenche o primeiro combo com todos os valores distintos de Level
                var distinctLevels = dataCables.AsEnumerable()
                                    .Select(row => row.Field<string>("Level"))
                                    .Where(level => !string.IsNullOrEmpty(level) && level != "GND")
                                    .Distinct()
                                    .ToList();

                cmbLevel.ItemsSource = distinctLevels;
            }
            finally
            {

                // Hide loading indicator
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ContentGrid.IsEnabled = true;
                // starts with the label and grounding size hidden
                lblGround.IsEnabled = false;
                cmbGround.IsEnabled = false;
            }
        }

        private void cmbLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpa os combos dependentes
            LimparCombosSequenciais(1);

            if (cmbLevel.SelectedItem != null)
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();

                var tiposDistintos = dataCables.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == levelSelecionado)
                                    .Select(row => row.Field<string>("Type"))
                                    .Where(type => !string.IsNullOrEmpty(type))
                                    .Distinct()
                                    .ToList();

                cmbType.ItemsSource = tiposDistintos;
            }
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpa os combos dependentes
            LimparCombosSequenciais(2);

            if (cmbLevel.SelectedItem != null && cmbType.SelectedItem != null)
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();

                var cablesDistintos = dataCables.AsEnumerable()
                                     .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                  row.Field<string>("Type") == typeSelecionado)
                                     .Select(row => row.Field<string>("Conductors"))
                                     .Where(cables => !string.IsNullOrEmpty(cables))
                                     .Distinct()
                                     .ToList();

                cmbConductors.ItemsSource = cablesDistintos;
            }
        }

        private void cmbConductors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpa os combos dependentes
            LimparCombosSequenciais(3);

            // Lógica do GIF - Mostra quando "Single" é selecionado
            if (cmbConductors.SelectedItem != null && cmbConductors.SelectedItem.ToString() == "Single")
            {
                checkTriplex.IsEnabled = true;
                checkTriplex.IsChecked = true;

                lblGround.IsEnabled = true;
                cmbGround.IsEnabled = true;

               
                var distinctGrounds = dataCables.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == "GND")
                                    .Select(row => row.Field<string>("Size"))
                                    .Where(cables => !string.IsNullOrEmpty(cables))
                                    .Distinct()
                                    .ToList();

                cmbGround.ItemsSource = distinctGrounds;


            }
            else
            {
                checkTriplex.IsEnabled = false;
                checkTriplex.IsChecked = false;
            }

            if (TemSelecaoCompleta(3))
            {
                ProcessarAmountMulti();
                ProcessarSize();
            }
        }



        private void cmbAmountMulti_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reprocessa o Size quando Amount Multi muda (se for Multiconductor)
            if (cmbConductors.SelectedItem?.ToString() == "Multiconductor" && cmbAmountMulti.SelectedItem != null)
            {
                ProcessarSizeComAmountMulti();
            }
        }

        private void ProcessarAmountMulti()
        {
            string conductorsSelecionado = cmbConductors.SelectedItem.ToString();

            if (conductorsSelecionado == "Multiconductor")
            {
                // Se for Multiconductor, preenche com valores da coluna Conductors
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();

                var quantidadeCondutores = dataCables.AsEnumerable()
                                          .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                       row.Field<string>("Type") == typeSelecionado &&
                                                       row.Field<string>("Conductors") == conductorsSelecionado)
                                          .Select(row => row.Field<string>("QtConductors"))
                                          .Where(conductors => !string.IsNullOrEmpty(conductors))
                                          .Distinct()
                                          .ToList();

                cmbAmountMulti.ItemsSource = quantidadeCondutores;
            }
            else
            {
                // Se não for Multiconductor, apenas o valor "1"
                cmbAmountMulti.ItemsSource = new List<string> { "1" };
                cmbAmountMulti.SelectedIndex = 0;
            }
        }

        private void ProcessarSize()
        {
            if (TemSelecaoCompleta(3))
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();
                string cablesSelecionado = cmbConductors.SelectedItem.ToString();

                var sizesDistintos = dataCables.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                 row.Field<string>("Type") == typeSelecionado &&
                                                 row.Field<string>("Conductors") == cablesSelecionado)
                                    .Select(row => row.Field<string>("Size"))
                                    .Where(size => !string.IsNullOrEmpty(size))
                                    .Distinct()
                                    .ToList();

                cmbSize.ItemsSource = sizesDistintos;

                foreach (var size in sizesDistintos)
                {
                    Debug.WriteLine($"- Size: '{size}'");
                }
            }
        }

        private void ProcessarSizeComAmountMulti()
        {
            if (TemSelecaoCompleta(3) && cmbAmountMulti.SelectedItem != null)
            {
                string levelSelecionado = cmbLevel.SelectedItem.ToString();
                string typeSelecionado = cmbType.SelectedItem.ToString();
                string cablesSelecionado = cmbConductors.SelectedItem.ToString();
                string amountSelecionado = cmbAmountMulti.SelectedItem.ToString();

                var sizesDistintos = dataCables.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == levelSelecionado &&
                                                 row.Field<string>("Type") == typeSelecionado &&
                                                 row.Field<string>("Conductors") == cablesSelecionado &&
                                                 row.Field<string>("QtConductors") == amountSelecionado)
                                    .Select(row => row.Field<string>("Size"))
                                    .Where(size => !string.IsNullOrEmpty(size))
                                    .Distinct()
                                    .ToList();

                cmbSize.ItemsSource = sizesDistintos;
                Debug.WriteLine($"Sizes para Multiconductor ({amountSelecionado} condutores): {sizesDistintos.Count}");
            }
        }

        // Métodos auxiliares
        private void LimparCombosSequenciais(int nivel)
        {
            switch (nivel)
            {
                case 1: // Limpa tudo após Level
                    cmbType.ItemsSource = null;
                    cmbType.SelectedIndex = -1;
                    
                    goto case 2;
                case 2: // Limpa tudo após Type
                    cmbConductors.ItemsSource = null;
                    cmbConductors.SelectedIndex = -1;
                    cmbGround.SelectedIndex = -1;
                    cmbGround.IsEnabled = false;
                    goto case 3;
                case 3: // Limpa tudo após Conductors
                    cmbAmountMulti.ItemsSource = null;
                    cmbAmountMulti.SelectedIndex = -1;
                    cmbSize.ItemsSource = null;
                    cmbSize.SelectedIndex = -1;
                    cmbGround.SelectedIndex = -1;
                    cmbGround.IsEnabled = false;
                    break;
            }
        }

        private bool TemSelecaoCompleta(int nivel)
        {
            switch (nivel)
            {
                case 3:
                    return cmbLevel.SelectedItem != null &&
                           cmbType.SelectedItem != null &&
                           cmbConductors.SelectedItem != null;
                case 4:
                    return TemSelecaoCompleta(3) && cmbAmountMulti.SelectedItem != null;
                default:
                    return false;
            }
        }

        
        private void btnAddCircuit_Click(object sender, RoutedEventArgs e)
        {
            DataRow newRow = elementsAdded.NewRow();

            // Preencher os valores das colunas
            newRow["Numb"] = elementsAdded.Rows.Count + 1;
            newRow["Level"] = cmbLevel.SelectedItem?.ToString() ?? string.Empty;
            newRow["Conductors"] = cmbConductors.SelectedItem?.ToString() ?? string.Empty;
            newRow["QtConductors"] = cmbAmountMulti.SelectedItem?.ToString() ?? string.Empty;
            newRow["Size"] = cmbSize.SelectedItem?.ToString() ?? string.Empty;
            newRow["Triplex"] = checkTriplex.IsChecked;
            newRow["Ground"] = cmbGround.SelectedItem?.ToString() ?? string.Empty;
            newRow["Type"] = cmbType.SelectedItem?.ToString() ?? string.Empty;

            // Adicionar a linha à tabela
            elementsAdded.Rows.Add(newRow);

            
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button?.Tag is DataRowView rowView)
            {
                // Confirmar exclusão
                MessageBoxResult result = MessageBox.Show(
                    "Tem certeza que deseja excluir esta linha?",
                    "Confirmar Exclusão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Remove da DataTable (o DataGrid será atualizado automaticamente)
                    rowView.Row.Delete();
                    elementsAdded.AcceptChanges();
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            elementsAdded.Rows.Clear();
            richConduit.Document = null;
        }

        private void btnSizer_Click(object sender, RoutedEventArgs e)
        {

            (string sized, FlowDocument elemento) sizedConduit = classCalc.sizeConduit(dataCables, dataConduits, cmbCondType.SelectedItem.ToString(), elementsAdded);


            txtSizedConduit.Visibility = Visibility.Visible;
            richConduit.Visibility = Visibility.Visible;

            txtSizedConduit.Text = "Sized: " + sizedConduit.sized;
            richConduit.Document = sizedConduit.elemento;
        }

        private void batchButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar seleção de tipo de conduit
            if (cmbCondType.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecione um tipo de conduit antes de processar o arquivo em lote.",
                               "Tipo de Conduit Necessário",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Configurar OpenFileDialog para selecionar arquivo Excel
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Selecionar arquivo Excel para processamento em lote",
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            // Mostrar dialog e verificar se usuário selecionou um arquivo
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Mostrar indicador de carregamento
                    LoadingIndicator.Visibility = Visibility.Visible;
                    ContentGrid.IsEnabled = false;

                    string conduitType = cmbCondType.SelectedItem.ToString();

                    // Criar processador de lote
                    var batchProcessor = new Conduits.BatchProcessor(dataCables, dataConduits);

                    // Processar arquivo (isso pode demorar para arquivos grandes)
                    string outputPath = batchProcessor.ProcessBatch(openFileDialog.FileName, conduitType);

                    // Notificar usuário sobre conclusão
                    MessageBoxResult result = MessageBox.Show(
                        $"Processamento em lote concluído com sucesso!\n\n" +
                        $"Arquivo original: {Path.GetFileName(openFileDialog.FileName)}\n" +
                        $"Arquivo com resultados: {Path.GetFileName(outputPath)}\n\n" +
                        $"O arquivo foi salvo em:\n{outputPath}\n\n" +
                        $"Deseja abrir o arquivo com os resultados?",
                        "Processamento Concluído",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = outputPath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception openEx)
                        {
                            MessageBox.Show($"Não foi possível abrir o arquivo automaticamente: {openEx.Message}\n\n" +
                                           $"Você pode abrir manualmente o arquivo em:\n{outputPath}",
                                           "Aviso",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Warning);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("O arquivo selecionado não foi encontrado. Verifique se o arquivo ainda existe no local especificado.",
                                   "Arquivo Não Encontrado",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Problema com o formato ou conteúdo do arquivo Excel:\n\n{ex.Message}\n\n" +
                                   "Verifique se:\n" +
                                   "• O arquivo é um Excel válido (.xlsx)\n" +
                                   "• A planilha contém dados na estrutura esperada\n" +
                                   "• Todas as colunas necessárias estão presentes",
                                   "Formato de Arquivo Inválido",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
                catch (DataException ex)
                {
                    MessageBox.Show($"Erro nos dados da planilha:\n\n{ex.Message}\n\n" +
                                   "Verifique se todos os dados estão preenchidos corretamente e se os valores " +
                                   "nas colunas correspondem aos dados esperados pelo sistema.",
                                   "Erro nos Dados",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Não foi possível acessar o arquivo. Verifique se:\n\n" +
                                   "• O arquivo não está aberto em outro programa\n" +
                                   "• Você tem permissão para ler/escrever no local do arquivo\n" +
                                   "• O arquivo não está protegido contra escrita",
                                   "Acesso Negado",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro inesperado durante o processamento:\n\n{ex.Message}\n\n" +
                                   "Se o problema persistir, verifique:\n" +
                                   "• Se o arquivo Excel está corrompido\n" +
                                   "• Se há espaço suficiente em disco\n" +
                                   "• Se todos os dados na planilha são válidos",
                                   "Erro Inesperado",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
                finally
                {
                    // Esconder indicador de carregamento
                    LoadingIndicator.Visibility = Visibility.Collapsed;
                    ContentGrid.IsEnabled = true;
                }
            }
        }


        #region Read Excel Files Auxiliars

        private DataTable CreateCircuitsDataTable()
        {
            DataTable dataTable = new DataTable("Circuits");

            // Adicionar colunas conforme estrutura do arquivo Excel
            dataTable.Columns.Add("Numb", typeof(int));
            dataTable.Columns.Add("Level", typeof(string));
            dataTable.Columns.Add("Type", typeof(string));
            dataTable.Columns.Add("Conductors", typeof(string));
            dataTable.Columns.Add("Size", typeof(string));
            dataTable.Columns.Add("QtConductors", typeof(int));
            dataTable.Columns.Add("Ground", typeof(string));
            dataTable.Columns.Add("Triplex", typeof(string));
            dataTable.Columns.Add("Conduit", typeof(string));

            // Permitir valores nulos para as colunas Ground e Triplex
            dataTable.Columns["Ground"].AllowDBNull = true;
            dataTable.Columns["Triplex"].AllowDBNull = true;

            return dataTable;
        }

        private T GetCellValue<T>(ExcelWorksheet worksheet, int row, int col, T defaultValue)
        {
            try
            {
                var cellValue = worksheet.Cells[row, col].Value;

                if (cellValue == null)
                    return defaultValue;

                // Conversão específica para int
                if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(cellValue.ToString(), out int intResult))
                        return (T)(object)intResult;
                    return defaultValue;
                }

                // Conversão para string
                if (typeof(T) == typeof(string))
                {
                    string stringValue = cellValue.ToString()?.Trim();
                    return string.IsNullOrEmpty(stringValue) ? defaultValue : (T)(object)stringValue;
                }

                // Conversão genérica
                return (T)Convert.ChangeType(cellValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        private void btnPattern_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Nome do arquivo e pasta
                string nomeArquivo = "CircuitsUpload.xlsx";
                string nomePasta = "Conduits";

                // Caminho completo do arquivo na pasta Conduits
                string caminhoOrigem = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nomePasta, nomeArquivo);

                // Verificar se o arquivo existe
                if (!File.Exists(caminhoOrigem))
                {
                    MessageBox.Show($"Arquivo '{nomeArquivo}' não encontrado na pasta '{nomePasta}'!",
                                  "Arquivo não encontrado",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    return;
                }

                // Abrir dialog para o usuário escolher onde salvar
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    FileName = nomeArquivo,
                    Filter = "Arquivos Excel (*.xlsx)|*.xlsx|Todos os arquivos (*.*)|*.*",
                    Title = "Salvar CircuitsUpload.xlsx como...",
                    DefaultExt = "xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Copiar arquivo para o destino escolhido
                    File.Copy(caminhoOrigem, saveDialog.FileName, true);

                    MessageBox.Show("Arquivo CircuitsUpload.xlsx baixado com sucesso!",
                                  "Download Concluído",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Acesso negado. Verifique as permissões do arquivo ou se ele não está sendo usado por outro programa.",
                              "Erro de Acesso",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("A pasta 'Conduits' não foi encontrada na aplicação.",
                              "Pasta não encontrada",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao baixar arquivo: {ex.Message}",
                              "Erro",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }
    }
}