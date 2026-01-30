using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    public class ReportGeneratorController : Controller
    {
        private readonly ReportService _reportService = new ReportService();

        // GET: ReportGenerator
        public ActionResult Index()
        {
            return View("~/Views/Reports/Index.cshtml");
        }

        // POST: ReportGenerator/GenerateDirect
        [HttpPost]
        public ActionResult GenerateDirect(string reportType, string format = "csv")
        {
            try
            {
                if (string.IsNullOrEmpty(reportType))
                {
                    return Json(new { success = false, message = "Please select a report type" });
                }

                var filePath = _reportService.GenerateReportByType(reportType, User.Identity.Name, format);
                var fileName = Path.GetFileName(filePath);
                
                return Json(new { 
                    success = true, 
                    message = $"Report '{fileName}' generated successfully",
                    fileName = fileName,
                    filePath = filePath
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error generating report: {ex.Message}" });
            }
        }

        // GET: ReportGenerator/Download?fileName=...
        public ActionResult Download(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return HttpNotFound();
                }

                var filePath = Path.Combine(Server.MapPath("~/Reports"), fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return HttpNotFound();
                }

                return File(filePath, "text/csv", fileName);
            }
            catch
            {
                return HttpNotFound();
            }
        }
    }
}
