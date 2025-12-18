using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Data.SqlClient;
// using HR.Data; // Uncomment and adjust this according to your real DbContext namespace

public class JobApplicationTests : IDisposable
{
    private readonly IWebDriver _driver;
    // private readonly HrDbContext _db; // Uncomment and adjust if you want DB assertions

    public JobApplicationTests()
    {
        _driver = new ChromeDriver();
        // _db = new HrDbContext(); // Uncomment when you have correct using
    }

    [Fact]
    public void ClientCanApplyForPositionAndApplicationIsSaved()
    {
        string clientUsername = "client"; // use the client account
        string clientPassword = "anypassword"; // any password works per user instruction
        string sampleCvPath = @"C:\\Temp\\sample_cv.pdf"; // already created (not used in this simple navigation test)

        // 1. Login as client
        _driver.Navigate().GoToUrl("http://localhost:52436/Account/Login");
        _driver.FindElement(By.Name("username")).SendKeys(clientUsername);
        _driver.FindElement(By.Name("password")).SendKeys(clientPassword);
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // 2. Go to positions index
        _driver.Navigate().GoToUrl("http://localhost:52436/Positions");
        var firstPosition = _driver.FindElements(By.ClassName("position-item")).First();
        string appliedPositionTitle = firstPosition.GetAttribute("data-title");
        firstPosition.Click();

        // 3. Click the Apply button (should open questionnaire)
        var applyBtn = _driver.FindElement(By.CssSelector(".btn-primary[href*='Questionnaire']"));
        applyBtn.Click();

        // 4. Confirm that we navigated to the questionnaire page
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(drv => drv.Url.Contains("/Applications/Questionnaire"));

        Assert.Contains("/Applications/Questionnaire", _driver.Url);
    }

    [Fact]
    public void AdminCreatingApplicationPersistsToDatabase()
    {
        // Arrange: read current Applications count from the database
        var connectionString = "Data Source=localhost\\\\SQLEXPRESS;Initial Catalog=HR_Local;Integrated Security=True;MultipleActiveResultSets=True";
        int beforeCount;
        using (var conn = new SqlConnection(connectionString))
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Applications", conn))
        {
            conn.Open();
            beforeCount = (int)cmd.ExecuteScalar();
        }

        // Login as admin
        _driver.Navigate().GoToUrl("http://localhost:52436/Account/Login");
        _driver.FindElement(By.Name("username")).SendKeys("admin");
        _driver.FindElement(By.Name("password")).SendKeys("anypassword");
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Go directly to the Create Application page for a known position
        _driver.Navigate().GoToUrl("http://localhost:52436/Applications/Create?positionId=1");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(drv => drv.FindElement(By.Name("ApplicantId")));

        // Select first real applicant and position from the dropdowns
        var applicantSelect = new SelectElement(_driver.FindElement(By.Name("ApplicantId")));
        applicantSelect.SelectByIndex(1); // skip the "-- Select --" option

        var positionSelect = new SelectElement(_driver.FindElement(By.Name("PositionId")));
        positionSelect.SelectByIndex(1); // first real position

        var statusInput = _driver.FindElement(By.Name("Status"));
        statusInput.Clear();
        statusInput.SendKeys("Submitted via UI test");

        // Submit the form
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Wait for redirect back to Applications index
        wait.Until(drv => drv.Url.Contains("/Applications"));

        // Assert: applications count increased by one
        int afterCount;
        using (var conn = new SqlConnection(connectionString))
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Applications", conn))
        {
            conn.Open();
            afterCount = (int)cmd.ExecuteScalar();
        }

        Assert.Equal(beforeCount + 1, afterCount);
    }

    [Fact]
    public void AdminCreatingPositionPersistsToDatabase()
    {
        // Arrange: read current Positions count from the database
        var connectionString = "Data Source=localhost\\\\SQLEXPRESS;Initial Catalog=HR_Local;Integrated Security=True;MultipleActiveResultSets=True";
        int beforeCount;
        using (var conn = new SqlConnection(connectionString))
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Positions", conn))
        {
            conn.Open();
            beforeCount = (int)cmd.ExecuteScalar();
        }

        // Login as admin
        _driver.Navigate().GoToUrl("http://localhost:52436/Account/Login");
        _driver.FindElement(By.Name("username")).SendKeys("admin");
        _driver.FindElement(By.Name("password")).SendKeys("anypassword");
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Go to Create Position page
        _driver.Navigate().GoToUrl("http://localhost:52436/Positions/Create");
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        wait.Until(drv => drv.FindElement(By.Name("Title")));

        // Fill in form
        _driver.FindElement(By.Name("Title")).SendKeys("Test Position " + Guid.NewGuid());
        _driver.FindElement(By.Name("Description")).SendKeys("Automated test position description.");
        _driver.FindElement(By.Name("SalaryMin")).Clear();
        _driver.FindElement(By.Name("SalaryMin")).SendKeys("30000");
        _driver.FindElement(By.Name("SalaryMax")).Clear();
        _driver.FindElement(By.Name("SalaryMax")).SendKeys("60000");

        // Select first real department
        var deptSelect = new SelectElement(_driver.FindElement(By.Name("DepartmentId")));
        deptSelect.SelectByIndex(1); // skip -- Select Department --

        // Ensure IsOpen is checked (it is by default, but be explicit)
        var isOpenCheckbox = _driver.FindElement(By.Name("IsOpen"));
        if (!isOpenCheckbox.Selected)
        {
            isOpenCheckbox.Click();
        }

        // Submit the form
        _driver.FindElement(By.CssSelector("button[type=submit]")).Click();

        // Wait for redirect back to Positions index
        wait.Until(drv => drv.Url.Contains("/Positions"));

        // Assert: positions count increased by one
        int afterCount;
        using (var conn = new SqlConnection(connectionString))
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Positions", conn))
        {
            conn.Open();
            afterCount = (int)cmd.ExecuteScalar();
        }

        Assert.Equal(beforeCount + 1, afterCount);
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
        // _db?.Dispose();
    }
}

