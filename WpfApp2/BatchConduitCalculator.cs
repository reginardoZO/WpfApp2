using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp2
{
    // Helper class to store the results for each conduit tag
    public class BatchConduitResult
    {
        public string ConduitTag { get; set; }
        public string CalculatedConduitSize { get; set; }
    }

    // Represents a conduit size entry from the database
    // Assuming this class is defined elsewhere, possibly in a Models folder or similar
    // For now, let's define a minimal version here for compilation
    public class ConduitSize
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double AreaIN { get; set; }
        // Add other properties if needed, e.g., Id
    }

    // Represents a cable entry
    // Assuming this class is defined elsewhere, possibly in a Models folder or similar
    // For now, let's define a minimal version here for compilation
    public class Cable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public double OD { get; set; } // Outer Diameter
        public bool IsTriplex { get; set; }
        public double GroundOD { get; set; } // Ground Outer Diameter
        // Add other properties as needed
    }

    // Placeholder for DatabaseAccess class
    // Assuming this class is defined elsewhere and provides database interaction
    public class DatabaseAccess
    {
        public DataTable ExecuteQuery(string query)
        {
            // This is a placeholder. In a real application, this method would
            // connect to the database, execute the query, and return a DataTable.
            Console.WriteLine($"Executing query: {query}");
            // Return an empty DataTable for now to avoid null reference errors during planning
            return new DataTable();
        }
    }

    // Placeholder for ConduitCalculator class
    // Assuming this class is defined elsewhere and provides conduit calculation logic
    public class ConduitCalculator
    {
        public class ConduitCalculationResult
        {
            public string ConduitName { get; set; }
            // Other properties like FillPercentage, ErrorMessage etc.
        }

        public ConduitCalculationResult CalculateConduit(List<Cable> cables, List<ConduitSize> allConduitSizes, string selectedConduitType)
        {
            // This is a placeholder. In a real application, this method would
            // perform the conduit calculation based on the provided cables, available sizes, and type.
            Console.WriteLine($"Calculating conduit for {cables.Count} cables of type {selectedConduitType} using {allConduitSizes.Count} conduit sizes.");
            if (cables.Any() && allConduitSizes.Any(cs => cs.Type == selectedConduitType))
            {
                 return new ConduitCalculationResult { ConduitName = $"{selectedConduitType}-CalculatedSize" };
            }
            return new ConduitCalculationResult { ConduitName = "N/A - Calculation Error" };
        }
    }


    public class BatchConduitCalculator
    {
        // Define constants for column names to avoid magic strings
        // These were based on the problem description
        private const string ConduitTagColumnName = "Conduit"; // Assumed from "Conduit" column for tags
        private const string CableIdColumnName = "ID";
        private const string CableNameColumnName = "Name";
        private const string CableOdColumnName = "OD";
        private const string CableIsTriplexColumnName = "IsTriplex";
        private const string CableGroundOdColumnName = "GroundOD";

        public List<BatchConduitResult> ProcessBatchFile(DataTable inputDataTable, string selectedConduitType)
        {
            var results = new List<BatchConduitResult>();

            // Handle empty input DataTable
            if (inputDataTable == null || inputDataTable.Rows.Count == 0)
            {
                Console.WriteLine("Input DataTable is empty. No processing will be done.");
                // Optionally, return a specific message or just an empty list.
                // results.Add(new BatchConduitResult { ConduitTag = "SYSTEM_INFO", CalculatedConduitSize = "Input data was empty." });
                return results; // Return empty list
            }

            List<ConduitSize> allConduitSizes;
            try
            {
                // 2. Fetch all conduit sizes from the database
                DatabaseAccess dbAccess = new DatabaseAccess();
                DataTable conduitSizesTable = dbAccess.ExecuteQuery("SELECT Name, Type, AreaIN FROM conduitsSizes");
                allConduitSizes = ConvertDataTableToConduitSizeList(conduitSizesTable);

                if (allConduitSizes == null || !allConduitSizes.Any())
                {
                    Console.WriteLine("Critical Error: Conduit sizes list is null or empty after database query and conversion. Batch processing aborted.");
                    results.Add(new BatchConduitResult { ConduitTag = "SYSTEM_ERROR", CalculatedConduitSize = "Failed to load essential conduit data from database. Batch processing aborted." });
                    return results;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error during database access or conduit size conversion: {ex.Message}\nStackTrace: {ex.StackTrace}");
                results.Add(new BatchConduitResult { ConduitTag = "SYSTEM_ERROR", CalculatedConduitSize = $"Failed to load essential conduit data: {ex.Message}" });
                return results;
            }

            // 3. Group the rows in inputDataTable by the "Conduit TAG" column
            var groupedByConduitTag = inputDataTable.AsEnumerable()
                .Where(row => row != null && row[ConduitTagColumnName] != DBNull.Value) // Ensure row and tag are not null
                .GroupBy(row => row.Field<string>(ConduitTagColumnName));

            // 4. For each group:
            foreach (var group in groupedByConduitTag)
            {
                string currentConduitTag = group.Key;

                // a. Create a List<Cable> from the DataRows in the current group
                List<Cable> cablesForThisGroup = ConvertDataRowsToCableList(group.ToList(), currentConduitTag);

                // b. Instantiate ConduitCalculator
                ConduitCalculator conduitCalculator = new ConduitCalculator();

                // c. Call conduitCalculator.CalculateConduit
                ConduitCalculator.ConduitCalculationResult calculationResult = conduitCalculator.CalculateConduit(cablesForThisGroup, allConduitSizes, selectedConduitType);

                // d. Create a BatchConduitResult object and add it to the list
                results.Add(new BatchConduitResult
                {
                    ConduitTag = currentConduitTag,
                    CalculatedConduitSize = calculationResult.ConduitName
                });
            }

            // 5. Return the list of BatchConduitResult objects
            return results;
        }

        // Utility method similar to CalculadoraView.ConvertDataTableToConduitSizeList
        private List<ConduitSize> ConvertDataTableToConduitSizeList(DataTable dt)
        {
            if (dt == null) return new List<ConduitSize>();

            var conduitSizes = new List<ConduitSize>();
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    conduitSizes.Add(new ConduitSize
                    {
                        Name = row.Field<string>("Name"),
                        Type = row.Field<string>("Type"),
                        AreaIN = row.Field<double>("AreaIN")
                        // Add other properties if they exist in the table and class
                    });
                }
                catch (Exception ex)
                {
                    // Log error or handle missing/incorrectly formatted columns
                    Console.WriteLine($"Error converting DataRow to ConduitSize: {ex.Message}");
                    // Optionally skip this entry or add a default/error entry
                }
            }
            return conduitSizes;
        }

        // Utility method similar to CalculadoraView.ConvertDataTableToCableList
        // This one takes List<DataRow> because we are processing grouped data
        // Added conduitTag parameter for more specific logging
        private List<Cable> ConvertDataRowsToCableList(List<DataRow> dataRows, string conduitTagForLogging)
        {
            if (dataRows == null) return new List<Cable>();

            var cables = new List<Cable>();
            foreach (DataRow row in dataRows)
            {
                try
                {
                    // Perform null checks for individual fields before attempting to convert
                    // This makes error messages more precise if a specific field is the issue
                    if (row == null)
                    {
                        Console.WriteLine($"Skipping null DataRow for Conduit TAG [{conduitTagForLogging}].");
                        continue;
                    }

                    // Example of checking critical fields for DBNull before Field<T>()
                    // Field<T>() handles DBNull by returning default(T), which might be acceptable for some non-critical nullable fields
                    // but for critical ones like ID or OD, we might want to log and skip.
                    if (row.IsNull(CableIdColumnName) || row.IsNull(CableOdColumnName))
                    {
                        Console.WriteLine($"Error converting DataRow to Cable for Conduit TAG [{conduitTagForLogging}]: ID or OD is DBNull. Row data: {string.Join(", ", row.ItemArray)}. Skipping this cable entry.");
                        continue;
                    }

                    cables.Add(new Cable
                    {
                        ID = row.Field<int>(CableIdColumnName),
                        Name = row.Field<string>(CableNameColumnName) ?? string.Empty, // Provide default for nullable string
                        OD = row.Field<double>(CableOdColumnName),
                        IsTriplex = row.Field<bool>(CableIsTriplexColumnName),
                        GroundOD = row.Field<double>(CableGroundOdColumnName)
                    });
                }
                catch (InvalidCastException ice)
                {
                     Console.WriteLine($"Invalid cast error converting DataRow to Cable for Conduit TAG [{conduitTagForLogging}]: {ice.Message}. Row data: {string.Join(", ", row.ItemArray)}. Ensure data types in Excel match expected types (e.g., OD is number, IsTriplex is TRUE/FALSE). Skipping this cable entry.");
                }
                catch (Exception ex) // Catch other potential exceptions like FormatException
                {
                    Console.WriteLine($"Error converting DataRow to Cable for Conduit TAG [{conduitTagForLogging}]: {ex.Message}. Row data: {string.Join(", ", row.ItemArray)}. Skipping this cable entry.");
                }
            }
            return cables;
        }
    }
}
