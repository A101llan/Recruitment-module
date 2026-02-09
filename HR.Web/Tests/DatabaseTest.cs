using System;
using System.Data.Entity;
using HR.Web.Data;

namespace HR.Web.Tests
{
    public class DatabaseTest
    {
        public static bool TestConnection()
        {
            try
            {
                using (var context = new HrContext())
                {
                    // Try to connect to the database
                    context.Database.Connection.Open();
                    context.Database.Connection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Database connection failed: {0}", ex.Message));
                return false;
            }
        }
        
        public static bool CreateDatabase()
        {
            try
            {
                using (var context = new HrContext())
                {
                    // Create the database if it doesn't exist
                    if (!context.Database.Exists())
                    {
                        context.Database.Create();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Database creation failed: {0}", ex.Message));
                return false;
            }
        }
    }
}
