using System;
using System.Collections.Generic;
using System.Data; // Added
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // Added
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
    }
}
