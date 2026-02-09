using System;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Tests
{
    public class PositionTest
    {
        public static void AddTestPosition()
        {
            using (var uow = new UnitOfWork())
            {
                // Get a valid department
                var department = uow.Departments.GetAll().FirstOrDefault();
                if (department == null)
                {
                    throw new Exception("No department found. Please add a department first.");
                }
                var position = new Position
                {
                    Title = "Test Position (Automated)",
                    Description = "This is a test position added by an automated test.",
                    SalaryMin = 30000,
                    SalaryMax = 50000,
                    PostedOn = DateTime.UtcNow,
                    IsOpen = true,
                    DepartmentId = department.Id
                };
                uow.Positions.Add(position);
                uow.Complete();
                Console.WriteLine(string.Format("Test position added with ID: {0}", position.Id));
            }
        }
    }
}
