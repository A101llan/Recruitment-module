using System;
using System.Data.SqlClient;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

public class PositionCreationTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _connectionString =
        "Data Source=localhost\\\\SQLEXPRESS;Initial Catalog=HR_Local;Integrated Security=True;MultipleActiveResultSets=True";

    public PositionCreationTests()
    {
        _driver = new ChromeDriver();
    }

    [Fact]
    public void AdminCanCreatePosition_AndItAppearsInDatabaseAndUI()
    {
        // Arrange: count positions before
        int beforeCount;
        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Positions", conn))
        {
            conn.Open();
            beforeCount = (int)cmd.ExecuteScalar();
        }

        var uniqueTitle = "Automated Test Position " + Guid.NewGuid();

        // Login as admin
        _driver.Navigate().GoToUrl("http://localhost:52436/Account/Login");
        _driver.FindElement(By.Name("username")).SendKeys("admin");
        _driver.FindElement(By.Name("password")).SendKeys("anypassword");
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Go to Add New Position
        _driver.Navigate().GoToUrl("http://localhost:52436/Positions");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(drv => drv.Url.Contains("/Positions"));

        // Click "Add New Position"
        _driver.FindElement(By.LinkText("Add New Position")).Click();
        wait.Until(drv => drv.Url.Contains("/Positions/Create"));

        // Fill in the form
        _driver.FindElement(By.Name("Title")).SendKeys(uniqueTitle);
        _driver.FindElement(By.Name("Description")).SendKeys("Created from automated PositionCreationTests.");
        _driver.FindElement(By.Name("SalaryMin")).Clear();
        _driver.FindElement(By.Name("SalaryMin")).SendKeys("30000");
        _driver.FindElement(By.Name("SalaryMax")).Clear();
        _driver.FindElement(By.Name("SalaryMax")).SendKeys("60000");

        var deptSelect = new SelectElement(_driver.FindElement(By.Name("DepartmentId")));
        deptSelect.SelectByIndex(1); // skip -- Select Department --

        // Ensure IsOpen is checked
        var isOpenCheckbox = _driver.FindElement(By.Name("IsOpen"));
        if (!isOpenCheckbox.Selected)
        {
            isOpenCheckbox.Click();
        }

        // Submit
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Wait for redirect back to Positions index
        wait.Until(drv => drv.Url.Contains("/Positions"));

        // Assert DB row count increased
        int afterCount;
        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Positions", conn))
        {
            conn.Open();
            afterCount = (int)cmd.ExecuteScalar();
        }
        Assert.Equal(beforeCount + 1, afterCount);

        // Assert new position visible in the UI list
        var positionItems = _driver.FindElements(By.CssSelector(".position-item"));
        bool foundInUi = false;
        foreach (var item in positionItems)
        {
            var titleAttr = item.GetAttribute("data-title");
            if (string.Equals(titleAttr, uniqueTitle, StringComparison.OrdinalIgnoreCase))
            {
                foundInUi = true;
                break;
            }
        }

        Assert.True(foundInUi, "Newly created position was not found in the Positions list UI.");
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}


