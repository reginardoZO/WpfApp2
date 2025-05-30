using Microsoft.Data.Sqlite;
using System.Data;
using System; // For Exception handling

namespace WpfApp2
{
    public class DatabaseAccess
    {
        private const string ConnectionString = @"Data Source=\\tonsrv1.rna.linde.grp\ACAD\Temporary\Reginardo\elec.db";

        // Connection string and methods will be added in later steps.
        public DatabaseAccess() // Optional: constructor if needed later
        {
        }

        public DataTable ExecuteQuery(string sqlQuery)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqliteCommand command = new SqliteCommand(sqlQuery, connection))
                    {
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Basic error handling: print to console or log.
                // Depending on application requirements, this could be more sophisticated.
                Console.WriteLine($"Error executing query: {ex.Message}");
                // Optionally, rethrow, return null, or an empty DataTable with error info.
                // For now, returning an empty DataTable on error.
            }
            return dataTable;
        }

        public int ExecuteNonQuery(string sqlCommand)
        {
            int rowsAffected = -1; // Default to -1 to indicate error or no operation
            try
            {
                using (SqliteConnection connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqliteCommand command = new SqliteCommand(sqlCommand, connection))
                    {
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Basic error handling: print to console or log.
                Console.WriteLine($"Error executing non-query: {ex.Message}");
                // Depending on requirements, could rethrow or handle more gracefully.
                // rowsAffected remains -1 in case of error.
            }
            return rowsAffected;
        }
    }
}
