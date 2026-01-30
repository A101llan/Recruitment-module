using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Services
{
    public class ReportService
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

        public string GenerateReportByType(string reportType, string generatedBy, string format = "csv")
        {
            string filePath = "";
            
            switch (reportType.ToLower())
            {
                case "candidate":
                    filePath = GenerateCandidateReport(null, generatedBy, format);
                    break;
                case "application":
                    filePath = GenerateApplicationReport(null, generatedBy, format);
                    break;
                case "interview":
                    filePath = GenerateInterviewReport(null, generatedBy, format);
                    break;
                case "department":
                    filePath = GenerateDepartmentReport(null, generatedBy, format);
                    break;
                case "position":
                    filePath = GeneratePositionReport(null, generatedBy, format);
                    break;
                case "security":
                    filePath = GenerateSecurityReport(null, generatedBy, format);
                    break;
                default:
                    throw new ArgumentException($"Unsupported report type: {reportType}");
            }

            return filePath;
        }

        private string GenerateCandidateReport(Report report, string generatedBy, string format = "csv")
        {
            var candidates = _uow.Applicants.GetAll().ToList();
            var fileName = $"Candidates_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            if (format.ToLower() == "pdf")
            {
                GenerateCandidatePDF(candidates, filePath, generatedBy);
            }
            else
            {
                GenerateCandidateCSV(candidates, filePath);
            }

            return filePath;
        }

        private void GenerateCandidateCSV(List<Applicant> candidates, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,FullName,Email,Phone");
                foreach (var candidate in candidates)
                {
                    writer.WriteLine($"{candidate.Id},{candidate.FullName},{candidate.Email},{candidate.Phone}");
                }
            }
        }

        private void GenerateCandidatePDF(List<Applicant> candidates, string filePath, string generatedBy)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Candidates Report</title>
    <style>
        @page {{
            margin: 2cm;
            size: A4;
        }}
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 0;
            padding: 20px;
            line-height: 1.6;
            color: #2c3e50;
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #3498db;
            padding-bottom: 20px;
            margin-bottom: 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }}
        .header p {{
            margin: 5px 0 0 0;
            opacity: 0.9;
            font-size: 14px;
        }}
        .report-info {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid #3498db;
        }}
        .report-info strong {{
            color: #2c3e50;
        }}
        table {{ 
            border-collapse: collapse; 
            width: 100%; 
            margin-top: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            border-radius: 8px;
            overflow: hidden;
        }}
        th {{ 
            background: linear-gradient(135deg, #3498db, #2980b9);
            color: white;
            padding: 15px 12px;
            text-align: left;
            font-weight: 600;
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        td {{ 
            border: 1px solid #e9ecef;
            padding: 12px;
            text-align: left;
            font-size: 13px;
        }}
        tr:nth-child(even) {{ 
            background-color: #f8f9fa; 
        }}
        tr:hover {{ 
            background-color: #e3f2fd;
            transition: background-color 0.3s ease;
        }}
        .footer {{ 
            margin-top: 40px;
            padding: 30px 0;
            border-top: 2px solid #3498db;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
            background: #f8f9fa;
            border-radius: 8px;
        }}
        .logo-section {{
            margin-top: 20px;
            padding: 20px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .logo-placeholder {{
            width: 120px;
            height: 60px;
            background: linear-gradient(135deg, #667eea, #764ba2);
            border-radius: 8px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: bold;
            font-size: 16px;
            margin: 10px auto;
        }}
        .stats {{
            display: flex;
            justify-content: space-around;
            margin: 20px 0;
            flex-wrap: wrap;
        }}
        .stat-item {{
            background: white;
            padding: 15px;
            border-radius: 8px;
            text-align: center;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            min-width: 120px;
            margin: 5px;
        }}
        .stat-number {{
            font-size: 24px;
            font-weight: bold;
            color: #3498db;
        }}
        .stat-label {{
            font-size: 12px;
            color: #6c757d;
            margin-top: 5px;
        }}
        .watermark {{
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-45deg);
            font-size: 72px;
            color: rgba(52, 152, 219, 0.1);
            font-weight: bold;
            z-index: -1;
            pointer-events: none;
        }}
    </style>
</head>
<body>
    <div class='watermark'>HR SYSTEM</div>
    
    <div class='header'>
        <h1><i class='fas fa-users'></i> Candidates Report</h1>
        <p>Comprehensive Candidate Database Analysis</p>
    </div>
    
    <div class='report-info'>
        <p><strong>Generated on:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        <p><strong>Generated by:</strong> {generatedBy}</p>
        <p><strong>Report ID:</strong> RPT-CAND-{DateTime.Now:yyyyMMddHHmmss}</p>
    </div>

    <div class='stats'>
        <div class='stat-item'>
            <div class='stat-number'>{candidates.Count}</div>
            <div class='stat-label'>Total Candidates</div>
        </div>
        <div class='stat-item'>
            <div class='stat-number'>{candidates.Count(c => !string.IsNullOrEmpty(c.Email))}</div>
            <div class='stat-label'>With Email</div>
        </div>
        <div class='stat-item'>
            <div class='stat-number'>{candidates.Count(c => !string.IsNullOrEmpty(c.Phone))}</div>
            <div class='stat-label'>With Phone</div>
        </div>
    </div>
    
    <table>
        <thead>
            <tr>
                <th><i class='fas fa-hashtag'></i> ID</th>
                <th><i class='fas fa-user'></i> Full Name</th>
                <th><i class='fas fa-envelope'></i> Email</th>
                <th><i class='fas fa-phone'></i> Phone</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var candidate in candidates)
            {
                html += $@"
            <tr>
                <td><strong>{candidate.Id}</strong></td>
                <td>{candidate.FullName}</td>
                <td>{candidate.Email}</td>
                <td>{candidate.Phone}</td>
            </tr>";
            }

            html += $@"
        </tbody>
    </table>
    
    <div class='footer'>
        <div class='logo-section'>
            <div class='logo-placeholder'>HR SYSTEM</div>
            <p><strong>Human Resources Management System</strong></p>
            <p>Professional Recruitment & Candidate Management</p>
            <p style='margin-top: 15px; font-size: 11px; color: #999;'>
                Generated on {DateTime.Now:yyyy-MM-dd at HH:mm:ss} | Page 1 of 1
            </p>
        </div>
        <p style='margin-top: 20px;'>
            <strong>Confidential Document</strong> - For Internal Use Only
        </p>
        <p style='font-size: 10px; margin-top: 10px;'>
            {DateTime.Now.Year} HR Management System. All rights reserved.
        </p>
    </div>
</body>
</html>";

            File.WriteAllText(filePath.Replace(".pdf", ".html"), html);
            File.Move(filePath.Replace(".pdf", ".html"), filePath);
        }

        private string GenerateApplicationReport(Report report, string generatedBy, string format = "csv")
        {
            var applications = _uow.Applications.GetAll(a => a.Applicant, a => a.Position).ToList();
            var fileName = $"Applications_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            if (format.ToLower() == "pdf")
            {
                GenerateApplicationPDF(applications, filePath, generatedBy);
            }
            else
            {
                GenerateApplicationCSV(applications, filePath);
            }

            return filePath;
        }

        private void GenerateApplicationCSV(List<Application> applications, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,Applicant,Position,Status,AppliedDate,Score");
                foreach (var app in applications)
                {
                    writer.WriteLine($"{app.Id},{app.Applicant.FullName},{app.Position.Title},{app.Status},{app.AppliedOn:yyyy-MM-dd},{app.Score}");
                }
            }
        }

        private void GenerateApplicationPDF(List<Application> applications, string filePath, string generatedBy)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Applications Report</title>
    <style>
        @page {{
            margin: 2cm;
            size: A4;
        }}
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 0;
            padding: 20px;
            line-height: 1.6;
            color: #2c3e50;
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #e74c3c;
            padding-bottom: 20px;
            margin-bottom: 30px;
            background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%);
            color: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }}
        .header p {{
            margin: 5px 0 0 0;
            opacity: 0.9;
            font-size: 14px;
        }}
        .report-info {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid #e74c3c;
        }}
        .report-info strong {{
            color: #2c3e50;
        }}
        table {{ 
            border-collapse: collapse; 
            width: 100%; 
            margin-top: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            border-radius: 8px;
            overflow: hidden;
        }}
        th {{ 
            background: linear-gradient(135deg, #e74c3c, #c0392b);
            color: white;
            padding: 15px 12px;
            text-align: left;
            font-weight: 600;
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        td {{ 
            border: 1px solid #e9ecef;
            padding: 12px;
            text-align: left;
            font-size: 13px;
        }}
        tr:nth-child(even) {{ 
            background-color: #f8f9fa; 
        }}
        tr:hover {{ 
            background-color: #ffebee;
            transition: background-color 0.3s ease;
        }}
        .footer {{ 
            margin-top: 40px;
            padding: 30px 0;
            border-top: 2px solid #e74c3c;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
            background: #f8f9fa;
            border-radius: 8px;
        }}
        .logo-section {{
            margin-top: 20px;
            padding: 20px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .logo-placeholder {{
            width: 120px;
            height: 60px;
            background: linear-gradient(135deg, #e74c3c, #c0392b);
            border-radius: 8px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: bold;
            font-size: 16px;
            margin: 10px auto;
        }}
        .stats {{
            display: flex;
            justify-content: space-around;
            margin: 20px 0;
            flex-wrap: wrap;
        }}
        .stat-item {{
            background: white;
            padding: 15px;
            border-radius: 8px;
            text-align: center;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            min-width: 120px;
            margin: 5px;
        }}
        .stat-number {{
            font-size: 24px;
            font-weight: bold;
            color: #e74c3c;
        }}
        .stat-label {{
            font-size: 12px;
            color: #6c757d;
            margin-top: 5px;
        }}
        .watermark {{
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-45deg);
            font-size: 72px;
            color: rgba(231, 76, 60, 0.1);
            font-weight: bold;
            z-index: -1;
            pointer-events: none;
        }}
        .status-badge {{
            padding: 4px 8px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: bold;
            text-transform: uppercase;
        }}
        .status-pending {{ background: #fff3cd; color: #856404; }}
        .status-approved {{ background: #d4edda; color: #155724; }}
        .status-rejected {{ background: #f8d7da; color: #721c24; }}
    </style>
</head>
<body>
    <div class='watermark'>HR SYSTEM</div>
    
    <div class='header'>
        <h1><i class='fas fa-file-alt'></i> Applications Report</h1>
        <p>Job Applications Status & Analysis</p>
    </div>
    
    <div class='report-info'>
        <p><strong>Generated on:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        <p><strong>Generated by:</strong> {generatedBy}</p>
        <p><strong>Report ID:</strong> RPT-APP-{DateTime.Now:yyyyMMddHHmmss}</p>
    </div>

    <div class='stats'>
        <div class='stat-item'>
            <div class='stat-number'>{applications.Count}</div>
            <div class='stat-label'>Total Applications</div>
        </div>
        <div class='stat-item'>
            <div class='stat-number'>{applications.Count(a => a.Status == "Pending")}</div>
            <div class='stat-label'>Pending</div>
        </div>
        <div class='stat-item'>
            <div class='stat-number'>{applications.Count(a => a.Status == "Approved")}</div>
            <div class='stat-label'>Approved</div>
        </div>
        <div class='stat-item'>
            <div class='stat-number'>{applications.Count(a => a.Status == "Rejected")}</div>
            <div class='stat-label'>Rejected</div>
        </div>
    </div>
    
    <table>
        <thead>
            <tr>
                <th><i class='fas fa-hashtag'></i> ID</th>
                <th><i class='fas fa-user'></i> Applicant</th>
                <th><i class='fas fa-briefcase'></i> Position</th>
                <th><i class='fas fa-info-circle'></i> Status</th>
                <th><i class='fas fa-calendar'></i> Applied Date</th>
                <th><i class='fas fa-star'></i> Score</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var app in applications)
            {
                var statusClass = app.Status?.ToLower() switch
                {
                    "pending" => "status-pending",
                    "approved" => "status-approved",
                    "rejected" => "status-rejected",
                    _ => ""
                };
                
                html += $@"
            <tr>
                <td><strong>{app.Id}</strong></td>
                <td>{app.Applicant.FullName}</td>
                <td>{app.Position.Title}</td>
                <td><span class='status-badge {statusClass}'>{app.Status}</span></td>
                <td>{app.AppliedOn:yyyy-MM-dd}</td>
                <td>{app.Score}</td>
            </tr>";
            }

            html += $@"
        </tbody>
    </table>
    
    <div class='footer'>
        <div class='logo-section'>
            <div class='logo-placeholder'>HR SYSTEM</div>
            <p><strong>Human Resources Management System</strong></p>
            <p>Professional Application Tracking & Management</p>
            <p style='margin-top: 15px; font-size: 11px; color: #999;'>
                Generated on {DateTime.Now:yyyy-MM-dd at HH:mm:ss} | Page 1 of 1
            </p>
        </div>
        <p style='margin-top: 20px;'>
            <strong>Confidential Document</strong> - For Internal Use Only
        </p>
        <p style='font-size: 10px; margin-top: 10px;'>
            © {DateTime.Now.Year} HR Management System. All rights reserved.
        </p>
    </div>
</body>
</html>";

            File.WriteAllText(filePath.Replace(".pdf", ".html"), html);
            File.Move(filePath.Replace(".pdf", ".html"), filePath);
        }

        private string GenerateInterviewReport(Report report, string generatedBy, string format = "csv")
        {
            var interviews = _uow.Interviews.GetAll().ToList();
            var fileName = $"Interviews_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            if (format.ToLower() == "pdf")
            {
                GenerateInterviewPDF(interviews, filePath, generatedBy);
            }
            else
            {
                GenerateInterviewCSV(interviews, filePath);
            }

            return filePath;
        }

        private void GenerateInterviewCSV(List<Interview> interviews, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,ApplicationID,InterviewerID,ScheduledDate,Mode,Notes");
                foreach (var interview in interviews)
                {
                    writer.WriteLine($"{interview.Id},{interview.ApplicationId},{interview.InterviewerId},{interview.ScheduledAt:yyyy-MM-dd HH:mm},{interview.Mode},{interview.Notes}");
                }
            }
        }

        private void GenerateInterviewPDF(List<Interview> interviews, string filePath, string generatedBy)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Interviews Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #2c3e50; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <h1>Interviews Report</h1>
    <p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    <p>Generated by: {generatedBy}</p>
    
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>Application ID</th>
                <th>Interviewer ID</th>
                <th>Scheduled Date</th>
                <th>Mode</th>
                <th>Notes</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var interview in interviews)
            {
                html += $@"
            <tr>
                <td>{interview.Id}</td>
                <td>{interview.ApplicationId}</td>
                <td>{interview.InterviewerId}</td>
                <td>{interview.ScheduledAt:yyyy-MM-dd HH:mm}</td>
                <td>{interview.Mode}</td>
                <td>{interview.Notes}</td>
            </tr>";
            }

            html += @"
        </tbody>
    </table>
    
    <div class='footer'>
        <p>Total Interviews: " + interviews.Count + @"</p>
        <p>HR System - Generated Report</p>
    </div>
</body>
</html>";

            File.WriteAllText(filePath.Replace(".pdf", ".html"), html);
            File.Move(filePath.Replace(".pdf", ".html"), filePath);
        }

        private string GenerateDepartmentReport(Report report, string generatedBy, string format = "csv")
        {
            var departments = _uow.Departments.GetAll(d => d.Positions).ToList();
            var fileName = $"Departments_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            if (format.ToLower() == "pdf")
            {
                GenerateDepartmentPDF(departments, filePath, generatedBy);
            }
            else
            {
                GenerateDepartmentCSV(departments, filePath);
            }

            return filePath;
        }

        private void GenerateDepartmentCSV(List<Department> departments, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,Name,Description,PositionCount");
                foreach (var dept in departments)
                {
                    writer.WriteLine($"{dept.Id},{dept.Name},{dept.Description},{dept.Positions?.Count ?? 0}");
                }
            }
        }

        private void GenerateDepartmentPDF(List<Department> departments, string filePath, string generatedBy)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Departments Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #2c3e50; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <h1>Departments Report</h1>
    <p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    <p>Generated by: {generatedBy}</p>
    
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Description</th>
                <th>Position Count</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var dept in departments)
            {
                html += $@"
            <tr>
                <td>{dept.Id}</td>
                <td>{dept.Name}</td>
                <td>{dept.Description}</td>
                <td>{dept.Positions?.Count ?? 0}</td>
            </tr>";
            }

            html += @"
        </tbody>
    </table>
    
    <div class='footer'>
        <p>Total Departments: " + departments.Count + @"</p>
        <p>HR System - Generated Report</p>
    </div>
</body>
</html>";

            File.WriteAllText(filePath.Replace(".pdf", ".html"), html);
            File.Move(filePath.Replace(".pdf", ".html"), filePath);
        }

        private string GeneratePositionReport(Report report, string generatedBy)
        {
            var positions = _uow.Positions.GetAll(p => p.Department, p => p.Applications).ToList();
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", $"Positions_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,Title,Department,IsOpen,ApplicationCount");
                foreach (var pos in positions)
                {
                    writer.WriteLine($"{pos.Id},{pos.Title},{pos.Department?.Name},{pos.IsOpen},{pos.Applications?.Count ?? 0}");
                }
            }

            return filePath;
        }

        private string GenerateSecurityReport(Report report, string generatedBy)
        {
            var auditLogs = _uow.AuditLogs.GetAll().ToList();
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", $"Security_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,Username,Action,Controller,Timestamp,IPAddress");
                foreach (var log in auditLogs)
                {
                    writer.WriteLine($"{log.Id},{log.Username},{log.Action},{log.Controller},{log.Timestamp:yyyy-MM-dd HH:mm},{log.IPAddress}");
                }
            }

            return filePath;
        }

        public List<Report> GetActiveReports()
        {
            return _uow.Reports.GetAll().Where(r => r.IsActive).ToList();
        }

        public Report GetReport(int id)
        {
            return _uow.Reports.Get(id);
        }
    }
}
