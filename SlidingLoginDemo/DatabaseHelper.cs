using System;
using System.Data;
using MySqlConnector;

namespace SlidingLoginDemo
{
    public static class DatabaseHelper
    {
        private const string ConnectionString = "Server=localhost;Port=3306;Database=social;User ID=root;Password=123456;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static int ExecuteNonQuery(string query, params MySqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static object? ExecuteScalar(string query, params MySqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            }
        }
    }
}
