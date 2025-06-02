using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace WpfApp2
{
    public class calc()
    {
        // Helper method to add a paragraph with optional bold run
        private void AddParagraph(FlowDocument doc, string text, bool isBold = false, bool addLineBreak = true)
        {
            Paragraph p = new Paragraph();
            Run run = new Run(text);
            if (isBold)
            {
                run.FontWeight = FontWeights.Bold;
            }
            p.Inlines.Add(run);
            if (addLineBreak)
            {
                p.Inlines.Add(new LineBreak());
            }
            doc.Blocks.Add(p);
        }

        // Helper method to add formatted text (e.g., "Label: Value")
        private void AddFormattedLine(Paragraph p, string label, string value, bool boldValue = false)
        {
            p.Inlines.Add(new Run(label + ": "));
            Run valueRun = new Run(value);
            if (boldValue)
            {
                valueRun.FontWeight = FontWeights.Bold;
            }
            p.Inlines.Add(valueRun);
            p.Inlines.Add(new LineBreak());
        }

        // conduit sizing area - All units are assumed to be in inches
        public (string sized, FlowDocument calculationDocument) sizeConduit(DataTable cableTable, DataTable conduitTable, string conduitType, DataTable elementsAdded)
        {
            FlowDocument calculationDoc = new FlowDocument();
            calculationDoc.PagePadding = new Thickness(10);
            calculationDoc.FontFamily = new FontFamily("Segoe UI");
            calculationDoc.FontSize = 12;

            AddParagraph(calculationDoc, "Starting Conduit Sizing Calculation", true, true);
            AddParagraph(calculationDoc, "(Units: inches)", false, true);

            double areaOcupadaSqIn = 0.0; // Occupied Area in square inches
            int qtCabos = 0; // Quantity of cables

            Paragraph initialInfo = new Paragraph();
            AddFormattedLine(initialInfo, "Total elements to add", elementsAdded.Rows.Count.ToString());
            calculationDoc.Blocks.Add(initialInfo);

            int elementIndex = 1;
            foreach (DataRow lineFromTable in elementsAdded.Rows)
            {
                AddParagraph(calculationDoc, $"--- Processing Element {elementIndex} ---", true, true);

                string level = lineFromTable["Level"].ToString();
                string type = lineFromTable["Type"].ToString();
                string conductors = lineFromTable["Conductors"].ToString();
                string size = lineFromTable["Size"].ToString();
                string qtCond = lineFromTable["QtConductors"].ToString();
                bool isTriplex = Convert.ToBoolean(lineFromTable["Triplex"]);
                string groundSize = isTriplex ? lineFromTable["Ground"].ToString() : "N/A";

                Paragraph elementDetails = new Paragraph();
                AddFormattedLine(elementDetails, "Level", level);
                AddFormattedLine(elementDetails, "Type", type);
                AddFormattedLine(elementDetails, "Conductors", conductors);
                AddFormattedLine(elementDetails, "Size", size);
                AddFormattedLine(elementDetails, "QtConductors", qtCond);
                AddFormattedLine(elementDetails, "Triplex", isTriplex.ToString());
                if (isTriplex) AddFormattedLine(elementDetails, "Ground Size", groundSize);
                calculationDoc.Blocks.Add(elementDetails);

                if (isTriplex)
                {
                    AddParagraph(calculationDoc, "Triplex type detected.", false, true);

                    // Fetch OD of the phase cable
                    AddParagraph(calculationDoc, "Fetching outer diameter (OD) of the phase cable (in inches)...", false, false);
                    var distinctCable = cableTable.AsEnumerable()
                                    .Where(row => row.Field<string>("Level") == level &&
                                                 row.Field<string>("Type") == type &&
                                                 row.Field<string>("Conductors") == conductors &&
                                                 row.Field<string>("Size") == size &&
                                                 row.Field<string>("QtConductors") == qtCond)
                                    .Select(row => row.Field<string>("OD")).FirstOrDefault();

                    if (distinctCable == null)
                    {
                        AddParagraph(calculationDoc, "ERROR: Phase cable not found for the given criteria.", true, false);
                        return ("Error", calculationDoc);
                    }
                    double odCableIn = Convert.ToDouble(distinctCable);
                    Paragraph phaseCablePara = new Paragraph();
                    AddFormattedLine(phaseCablePara, "Phase cable OD found", $"{odCableIn:F4} in", true);
                    double areaCableSqIn = Math.PI * Math.Pow((odCableIn / 2), 2);
                    AddFormattedLine(phaseCablePara, "Phase cable area calculation", $"PI * ({odCableIn:F4} / 2)^2 = {areaCableSqIn:F6} in²");
                    calculationDoc.Blocks.Add(phaseCablePara);

                    // Fetch OD of the ground cable
                    AddParagraph(calculationDoc, $"Fetching outer diameter (OD) of the ground cable (Size={groundSize}, in inches)...", false, false);
                    var groundCable = cableTable.AsEnumerable()
                        .Where(row => row.Field<string>("Level") == "GND" &&
                                row.Field<string>("Size") == groundSize)
                        .Select(row => row.Field<string>("OD")).FirstOrDefault();

                    if (groundCable == null)
                    {
                        AddParagraph(calculationDoc, $"ERROR: Ground cable not found for size {groundSize}.", true, false);
                        return ("Error", calculationDoc);
                    }
                    double odGroundIn = Convert.ToDouble(groundCable);
                    Paragraph groundCablePara = new Paragraph();
                    AddFormattedLine(groundCablePara, "Ground cable OD found", $"{odGroundIn:F4} in", true);
                    double areaGroundSqIn = Math.PI * Math.Pow((odGroundIn / 2), 2);
                    AddFormattedLine(groundCablePara, "Ground cable area calculation", $"PI * ({odGroundIn:F4} / 2)^2 = {areaGroundSqIn:F6} in²");
                    calculationDoc.Blocks.Add(groundCablePara);

                    double areaElementoTriplexSqIn = areaCableSqIn * 3 + areaGroundSqIn;
                    Paragraph triplexAreaPara = new Paragraph();
                    AddFormattedLine(triplexAreaPara, "Occupied area for Triplex element", $"(3 * {areaCableSqIn:F6}) + {areaGroundSqIn:F6} = {areaElementoTriplexSqIn:F6} in²");
                    calculationDoc.Blocks.Add(triplexAreaPara);

                    areaOcupadaSqIn += areaElementoTriplexSqIn;
                    qtCabos += 4; // 3 phases + 1 ground
                }
                else // Not Triplex
                {
                    AddParagraph(calculationDoc, "Single type detected.", false, true);
                    // Fetch OD of the cable
                    AddParagraph(calculationDoc, "Fetching outer diameter (OD) of the cable (in inches)...", false, false);
                    var distinctCable = cableTable.AsEnumerable()
                                   .Where(row => row.Field<string>("Level") == level &&
                                                row.Field<string>("Type") == type &&
                                                row.Field<string>("Conductors") == conductors &&
                                                row.Field<string>("Size") == size &&
                                                row.Field<string>("QtConductors") == qtCond)
                                   .Select(row => row.Field<string>("OD")).FirstOrDefault();

                    if (distinctCable == null)
                    {
                        AddParagraph(calculationDoc, "ERROR: Cable not found for the given criteria.", true, false);
                        return ("Error", calculationDoc);
                    }
                    double odCableIn = Convert.ToDouble(distinctCable);
                    Paragraph singleCablePara = new Paragraph();
                    AddFormattedLine(singleCablePara, "Cable OD found", $"{odCableIn:F4} in", true);
                    double areaCableSqIn = Math.PI * Math.Pow((odCableIn / 2), 2);
                    AddFormattedLine(singleCablePara, "Cable area calculation", $"PI * ({odCableIn:F4} / 2)^2 = {areaCableSqIn:F6} in²");
                    calculationDoc.Blocks.Add(singleCablePara);

                    areaOcupadaSqIn += areaCableSqIn;
                    qtCabos += 1;
                }

                Paragraph accumulatedPara = new Paragraph();
                AddFormattedLine(accumulatedPara, "Accumulated occupied area", $"{areaOcupadaSqIn:F6} in²", true);
                AddFormattedLine(accumulatedPara, "Accumulated cable count", qtCabos.ToString(), true);
                calculationDoc.Blocks.Add(accumulatedPara);

                elementIndex++;
            }

            AddParagraph(calculationDoc, "--- End of element addition ---", true, true);
            Paragraph summaryPara = new Paragraph();
            AddFormattedLine(summaryPara, "Total occupied area by cables", $"{areaOcupadaSqIn:F6} in²", true);
            AddFormattedLine(summaryPara, "Total cable count", qtCabos.ToString(), true);
            calculationDoc.Blocks.Add(summaryPara);

            // Filter conduit table by type
            AddParagraph(calculationDoc, $"\nFiltering conduits by type: {conduitType}", true, true);
            DataTable conduitTableFiltered;
            try
            {
                AddParagraph(calculationDoc, "Ordering conduits by internal area (ascending, in²)...", false, false);
                conduitTableFiltered = conduitTable.AsEnumerable()
                        .Where(row => row.Field<string>("Type") == conduitType)
                        .OrderBy(row => Convert.ToDouble(row["areaIn"])) // Order by areaIn (in²)
                        .CopyToDataTable();
                AddParagraph(calculationDoc, $"{conduitTableFiltered.Rows.Count} conduit sizes found for type {conduitType}.", false, true);
            }
            catch (InvalidOperationException)
            {
                AddParagraph(calculationDoc, $"ERROR: No conduits found for type {conduitType}.", true, false);
                return ("Error", calculationDoc);
            }

            string selectedConduit = "";
            double factor = 0;

            // Determine fill factor
            AddParagraph(calculationDoc, "\nDetermining fill factor based on cable count...", true, true);
            Paragraph factorPara = new Paragraph();
            switch (qtCabos)
            {
                case 0:
                    AddParagraph(calculationDoc, "No cables added. Cannot size conduit.", true, false);
                    return ("N/A", calculationDoc);
                case 1:
                    factor = 0.53;
                    AddFormattedLine(factorPara, "Cable count = 1. Fill factor", factor.ToString("F2"), true);
                    break;
                case 2:
                    factor = 0.31;
                    AddFormattedLine(factorPara, "Cable count = 2. Fill factor", factor.ToString("F2"), true);
                    break;
                default: // More than 2 cables
                    factor = 0.40;
                    AddFormattedLine(factorPara, $"Cable count > 2 ({qtCabos}). Fill factor", factor.ToString("F2"), true);
                    break;
            }
            calculationDoc.Blocks.Add(factorPara);

            // Calculate minimum required internal area of the conduit in square inches
            double areaMinimaNecessariaSqIn = areaOcupadaSqIn / factor;
            Paragraph minAreaPara = new Paragraph();
            AddFormattedLine(minAreaPara, "Minimum required internal conduit area", $"Occupied Area / Factor = {areaOcupadaSqIn:F6} in² / {factor:F2} = {areaMinimaNecessariaSqIn:F6} in²", true);
            calculationDoc.Blocks.Add(minAreaPara);

            // Find the suitable conduit
            AddParagraph(calculationDoc, "\nSearching for a suitable conduit...", true, true);
            Paragraph searchPara = new Paragraph();
            foreach (DataRow row in conduitTableFiltered.Rows) // Already ordered by areaIn (in²)
            {
                double areaInternaConduitSqIn = Convert.ToDouble(row["areaIn"]);
                string conduitSize = row["Size"].ToString();
                searchPara.Inlines.Add(new LineBreak());
                searchPara.Inlines.Add(new Run($"Checking conduit: Size={conduitSize}, Internal Area={areaInternaConduitSqIn:F6} in²"));
                searchPara.Inlines.Add(new LineBreak());
                searchPara.Inlines.Add(new Run($"Comparing: Internal Area ({areaInternaConduitSqIn:F6} in²) >= Minimum Required Area ({areaMinimaNecessariaSqIn:F6} in²) ?"));
                searchPara.Inlines.Add(new LineBreak());

                if (areaInternaConduitSqIn >= areaMinimaNecessariaSqIn)
                {
                    selectedConduit = conduitSize;
                    Run foundRun = new Run($"Suitable conduit found! Selected size: {selectedConduit}");
                    foundRun.FontWeight = FontWeights.Bold;
                    foundRun.Foreground = Brushes.Green;
                    searchPara.Inlines.Add(foundRun);
                    searchPara.Inlines.Add(new LineBreak());
                    break; // Stop searching
                }
                else
                {
                    searchPara.Inlines.Add(new Run("Internal area insufficient. Checking next size..."));
                    searchPara.Inlines.Add(new LineBreak());
                }
            }
            calculationDoc.Blocks.Add(searchPara);

            if (string.IsNullOrEmpty(selectedConduit))
            {
                selectedConduit = "Overflow";
                Paragraph overflowPara = new Paragraph();
                Run overflowRun = new Run("\nNo suitable conduit found with sufficient internal area. Result: Overflow");
                overflowRun.FontWeight = FontWeights.Bold;
                overflowRun.Foreground = Brushes.Red;
                overflowPara.Inlines.Add(overflowRun);
                calculationDoc.Blocks.Add(overflowPara);
            }

            AddParagraph(calculationDoc, "\n--- Calculation Finished ---", true, false);

            return (selectedConduit, calculationDoc);
        }
    }
}
