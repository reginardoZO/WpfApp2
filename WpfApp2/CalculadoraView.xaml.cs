﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO; // Added for Stream and File operations
using OfficeOpenXml; // Added for EPPlus
using Microsoft.Win32; // Added for SaveFileDialog
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for CalculadoraView.xaml
    /// </summary>
    public partial class CalculadoraView : UserControl
    {
        public CalculadoraView()
        {
            InitializeComponent();
        }

        private List<Cable> ConvertDataTableToCableList(DataTable dataTable)
        {
            var cableList = new List<Cable>();
            if (dataTable == null) return cableList;

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    cableList.Add(new Cable(
                        id: row["ID"]?.ToString() ?? string.Empty,
                        name: row["Name"]?.ToString() ?? string.Empty,
                        od: Convert.ToDouble(row["OD"]), // Assuming OD is not null
                        isTriplex: Convert.ToBoolean(row["IsTriplex"]), // Assuming IsTriplex is not null
                        groundOD: row["GroundOD"] != DBNull.Value ? Convert.ToDouble(row["GroundOD"]) : 0.0
                    ));
                }
                catch (Exception ex)
                {
                    // Log error or handle missing/malformed columns
                    Console.WriteLine($"Error converting row to Cable: {ex.Message}");
                    // Optionally, skip this row or add a placeholder/default Cable object
                }
            }
            return cableList;
        }

        private List<ConduitSize> ConvertDataTableToConduitSizeList(DataTable dataTable)
        {
            var conduitList = new List<ConduitSize>();
            if (dataTable == null) return conduitList;

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    conduitList.Add(new ConduitSize(
                        name: row["Name"]?.ToString() ?? string.Empty,
                        type: row["Type"]?.ToString() ?? string.Empty,
                        areaIN: Convert.ToDouble(row["AreaIN"]) // Assuming AreaIN is not null
                    ));
                }
                catch (Exception ex)
                {
                    // Log error or handle missing/malformed columns
                    Console.WriteLine($"Error converting row to ConduitSize: {ex.Message}");
                    // Optionally, skip this row or add a placeholder/default ConduitSize object
                }
            }
            return conduitList;
        }

        public void PerformConduitCalculation(
            List<Cable> selectedCables,
            string selectedConduitType,
            TextBlock resultConduitNameTextBlock,
            RichTextBox resultStepsRichTextBox)
        {
            DatabaseAccess dbAccess = new DatabaseAccess();
            ConduitCalculator calculator = new ConduitCalculator();

            // Clear previous results
            resultConduitNameTextBlock.Text = "";
            resultStepsRichTextBox.Document.Blocks.Clear();

            // 1. Fetch all possible conduit sizes from DB
            // The 'cables' table is not fetched here as `selectedCables` is passed in.
            // If `selectedCables` were just IDs, then fetching all cables might be needed here.
            DataTable conduitSizesTable = dbAccess.ExecuteQuery("SELECT Name, Type, AreaIN FROM conduitsSizes");

            // 2. Convert DataTables to Lists of objects
            List<ConduitSize> allConduitSizes = ConvertDataTableToConduitSizeList(conduitSizesTable);

            // 3. Validate inputs (basic)
            if (selectedCables == null || !selectedCables.Any())
            {
                resultStepsRichTextBox.AppendText("Error: No cables selected for calculation.");
                resultConduitNameTextBlock.Text = "Error: No Cables";
                return;
            }
            if (string.IsNullOrWhiteSpace(selectedConduitType))
            {
                resultStepsRichTextBox.AppendText("Error: No conduit type selected.");
                resultConduitNameTextBlock.Text = "Error: No Type";
                return;
            }
            if (allConduitSizes == null || !allConduitSizes.Any())
            {
                // This could also indicate an issue with DB connection or the conduitsSizes table itself
                resultStepsRichTextBox.AppendText("Error: Could not load conduit sizes from the database, or no conduits of any type exist.");
                resultConduitNameTextBlock.Text = "Error: No Conduits";
                return;
            }

            // 4. Perform Calculation
            ConduitCalculatorResult result = calculator.CalculateConduit(selectedCables, allConduitSizes, selectedConduitType);

            // 5. Display Results
            resultConduitNameTextBlock.Text = result.ConduitName;
            // AppendText is fine for RichTextBox if no complex formatting is needed initially.
            // For more complex formatting, one might manipulate FlowDocument blocks directly.
            resultStepsRichTextBox.AppendText(result.CalculationSteps);
        }

        // New Batch Processing Logic
        private async void BatchProcessButton_Click(object sender, RoutedEventArgs e)
        {
            // This is a placeholder for where you'd get the DataTable.
            // In a real app, this might come from an OpenFileDialog and Excel parsing.
            DataTable uploadedDataTable = GetPlaceholderDataTable();

            if (uploadedDataTable == null || uploadedDataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to process. Please upload an Excel file first.", "Batch Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedConduitType = "EMT"; // Hardcoded as per subtask requirement

            try
            {
                BatchConduitCalculator batchCalculator = new BatchConduitCalculator();
                List<BatchConduitResult> batchResults = batchCalculator.ProcessBatchFile(uploadedDataTable, selectedConduitType);

                if (batchResults == null || !batchResults.Any())
                {
                    // Check if the first result (if any) indicates a system error from BatchConduitCalculator
                    if (batchResults != null && batchResults.Any() && batchResults[0].ConduitTag == "SYSTEM_ERROR")
                    {
                         MessageBox.Show(batchResults[0].CalculatedConduitSize, "Batch Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("No results were generated from the batch process. The input file might be empty or not in the expected format.", "Batch Process Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                ExportToExcel(uploadedDataTable, batchResults);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during batch processing: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Batch Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(DataTable originalData, List<BatchConduitResult> conduitResults)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Or LicenseContext.Commercial

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "Conduit_Batch_Results.xlsx",
                Title = "Save Batch Results"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(saveFileDialog.FileName);
                    using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
                    {
                        // Sheet 1: Original Data
                        ExcelWorksheet sheet1 = excelPackage.Workbook.Worksheets.Add("Original Input");
                        sheet1.Cells["A1"].LoadFromDataTable(originalData, true);
                        sheet1.Cells[sheet1.Dimension.Address].AutoFitColumns();

                        // Sheet 2: Conduit Calculation Results
                        ExcelWorksheet sheet2 = excelPackage.Workbook.Worksheets.Add("Conduit Calculation Results");
                        // Add headers
                        sheet2.Cells["A1"].Value = "Conduit TAG";
                        sheet2.Cells["B1"].Value = "Calculated Conduit Size";
                        sheet2.Cells["A1:B1"].Style.Font.Bold = true;

                        // Populate data
                        for (int i = 0; i < conduitResults.Count; i++)
                        {
                            sheet2.Cells[i + 2, 1].Value = conduitResults[i].ConduitTag;
                            sheet2.Cells[i + 2, 2].Value = conduitResults[i].CalculatedConduitSize;
                        }
                        sheet2.Cells[sheet2.Dimension.Address].AutoFitColumns();

                        excelPackage.Save();
                    } // The 'using' statement ensures the package is disposed correctly.

                    MessageBox.Show("Batch processing complete. Excel file saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                     MessageBox.Show($"Error saving Excel file: {ex.Message}", "Excel Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Batch processing was completed, but the results were not saved.", "Save Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Placeholder method to simulate getting a DataTable.
        // In a real application, this would involve reading an Excel file.
        private DataTable GetPlaceholderDataTable()
        {
            DataTable dt = new DataTable("UploadedData");
            // Define columns based on expected input structure for BatchConduitCalculator
            dt.Columns.Add("Conduit", typeof(string)); // Conduit TAG column
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("OD", typeof(double));
            dt.Columns.Add("IsTriplex", typeof(bool));
            dt.Columns.Add("GroundOD", typeof(double));

            // Add some sample data for testing
            dt.Rows.Add("C1", 1, "CableA", 0.5, false, 0.2);
            dt.Rows.Add("C1", 2, "CableB", 0.6, false, 0.2);
            dt.Rows.Add("C2", 3, "CableC", 0.7, true, 0.0); // Triplex, no separate ground OD in this example
            dt.Rows.Add("C2", 4, "CableD", 0.4, false, 0.15);
            dt.Rows.Add("C3", 5, "CableE", 0.8, false, 0.3);

            // Add a case that might lead to "No cable data for this tag" if not handled by BatchConduitCalculator's ConvertDataRowsToCableList
            // dt.Rows.Add("C4", DBNull.Value, null, DBNull.Value, DBNull.Value, DBNull.Value); // This would cause issues, ensure BatchConduitCalculator handles it

            // Add a tag with no cables to test that scenario
            // dt.Rows.Add("C5_NO_CABLES", DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value); // This setup is tricky, usually means no rows for C5

            return dt;
        }
    }
}
