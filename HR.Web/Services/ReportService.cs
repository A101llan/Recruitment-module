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
            string html = "";
            var fileName = $"{reportType}_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            switch (reportType.ToLower())
            {
                case "candidate":
                    if (format.ToLower() == "pdf") html = GenerateCandidatePDF(generatedBy);
                    else GenerateCandidateCSV(_uow.Applicants.GetAll().ToList(), filePath);
                    break;
                case "application":
                    if (format.ToLower() == "pdf") html = GenerateApplicationPDF(generatedBy);
                    else GenerateApplicationCSV(_uow.Applications.GetAll(a => a.Applicant, a => a.Position).ToList(), filePath);
                    break;
                case "interview":
                    if (format.ToLower() == "pdf") html = GenerateInterviewPDF(generatedBy);
                    else GenerateInterviewCSV(_uow.Interviews.GetAll().ToList(), filePath);
                    break;
                case "department":
                    if (format.ToLower() == "pdf") html = GenerateDepartmentPDF(generatedBy);
                    else GenerateDepartmentCSV(_uow.Departments.GetAll(d => d.Positions).ToList(), filePath);
                    break;
                case "position":
                    if (format.ToLower() == "pdf") html = GeneratePositionPDF(generatedBy);
                    else GeneratePositionCSV(_uow.Positions.GetAll(p => p.Department).ToList(), filePath);
                    break;
                case "security":
                    if (format.ToLower() == "pdf") html = GenerateSecurityPDF(generatedBy);
                    else GenerateSecurityCSV(_uow.AuditLogs.GetAll().ToList(), filePath);
                    break;
                default:
                    throw new ArgumentException($"Unsupported report type: {reportType}");
            }

            if (format.ToLower() == "pdf" && !string.IsNullOrEmpty(html))
            {
                File.WriteAllText(filePath, html);
            }

            return filePath;
        }

        public string PreviewReportByType(string reportType, string generatedBy)
        {
            switch (reportType.ToLower())
            {
                case "candidate": return GenerateCandidatePDF(generatedBy);
                case "application": return GenerateApplicationPDF(generatedBy);
                case "interview": return GenerateInterviewPDF(generatedBy);
                case "department": return GenerateDepartmentPDF(generatedBy);
                case "position": return GeneratePositionPDF(generatedBy);
                case "security": return GenerateSecurityPDF(generatedBy);
                default: return "<h3>Report type not supported for preview</h3>";
            }
        }

        private string GetReportStyles(string themeColor = "#3498db", string secondaryColor = "#2980b9")
        {
            return $@"
        @page {{
            margin: 2cm 2cm 3cm 2cm;
            size: A4;
        }}
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 0;
            padding: 0;
            line-height: 1.6;
            color: #2c3e50;
            background-color: #fff;
        }}
        .container {{
            padding: 0 40px 80px 40px;
        }}
        .report-frame {{
            border: 1px solid #e1e8ed;
            border-radius: 25px;
            margin: 0 0 50px 0;
            padding: 0;
            position: relative;
            background: #fff;
            box-shadow: 0 5px 15px rgba(0,0,0,0.03);
            overflow: hidden;
            border-top: 5px solid {themeColor};
            page-break-inside: avoid;
            display: block;
            width: 100%;
        }}
        .report-header-box {{
            text-align: center;
            background: linear-gradient(135deg, {themeColor} 0%, {secondaryColor} 100%);
            color: white;
            padding: 40px 20px;
            border-radius: 0 0 20px 20px;
            margin-bottom: 30px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        }}
        .report-header-box h1 {{
            margin: 0;
            font-size: 32px;
            font-weight: 600;
            letter-spacing: 1px;
            text-transform: uppercase;
        }}
        .report-header-box p {{
            margin: 10px 0 0 0;
            opacity: 0.9;
            font-size: 16px;
        }}
        .report-meta {{
            display: table;
            width: 100%;
            background: #f8f9fa;
            padding: 20px;
            border-radius: 12px;
            margin: 20px 0;
            border-left: 5px solid {themeColor};
            box-shadow: 0 2px 5px rgba(0,0,0,0.05);
        }}
        .meta-item {{
            display: table-cell;
            width: 33%;
        }}
        .meta-label {{
            font-size: 12px;
            color: #7f8c8d;
            text-transform: uppercase;
            font-weight: bold;
            display: block;
        }}
        .meta-value {{
            font-size: 15px;
            color: #2c3e50;
            font-weight: 600;
        }}
        .report-table {{ 
            border-collapse: separate;
            border-spacing: 0;
            width: 100%; 
            margin-top: 30px;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.05);
            border: 1px solid #e1e8ed;
        }}
        .report-table th {{ 
            background-color: {themeColor};
            color: white !important;
            padding: 18px 15px;
            text-align: left;
            font-weight: 600 !important;
            font-size: 13px !important;
            text-transform: uppercase !important;
            letter-spacing: 1px;
            border: none;
        }}
        .report-table td {{ 
            border-bottom: 1px solid #f1f4f6;
            padding: 15px;
            text-align: left;
            font-size: 14px;
            color: #444;
        }}
        .report-table tr:last-child td {{
            border-bottom: none;
        }}
        .report-table tr:nth-child(even) {{ 
            background-color: #fafbfc; 
        }}
        .report-table tr {{
            page-break-inside: avoid;
        }}
        .status-badge {{
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 11px;
            font-weight: bold;
            text-transform: uppercase;
            display: inline-block;
        }}
        .badge-success {{ background: #d4edda; color: #155724; }}
        .badge-warning {{ background: #fff3cd; color: #856404; }}
        .badge-danger {{ background: #f8d7da; color: #721c24; }}
        .report-footer {{ 
            width: 100%;
            text-align: center;
            font-size: 11px;
            color: #95a5a6;
            padding: 25px 0;
            margin-top: 40px;
            border-radius: 0 0 25px 25px;
            background: #fcfcfc;
            border: 1px solid #e1e8ed;
        }}
        .page-number:after {{
            content: ""Page "" counter(page);
        }}
        @media print {{
            .page-break {{ 
                display: block; 
                page-break-before: always; 
                margin: 40px 0;
                height: 30px;
                border: 1px solid #e1e8ed;
                border-radius: 0 0 25px 25px;
                background: #fff;
            }}
            .report-footer {{
                position: fixed;
                bottom: 0;
                border-radius: 0 0 25px 25px;
            }}
        }}
        .pagination-info {{
            text-align: right;
            font-size: 12px;
            color: #7f8c8d;
            margin-bottom: 5px;
            font-weight: 600;
        }}
        .stats-grid {{
            display: table;
            width: 100%;
            border-spacing: 20px;
            margin: 10px -20px;
        }}
        .stat-card-cell {{
            display: table-cell;
            background: white;
            padding: 20px;
            border-radius: 15px;
            text-align: center;
            border: 1px solid #ecf0f1;
            width: 25%;
        }}
        .stat-value {{
            font-size: 28px;
            font-weight: 700;
            color: {themeColor};
            display: block;
        }}
        .stat-label {{
            font-size: 12px;
            color: #7f8c8d;
            text-transform: uppercase;
            font-weight: bold;
        }}
";
        }

        private string GetReportHeader(string title, string subtitle, string themeColor = "#3498db", string secondaryColor = "#2980b9")
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
    <style>{GetReportStyles(themeColor, secondaryColor)}</style>
</head>
<body>
    <div class='container'>";
        }

        private string GetReportMeta(string generatedBy, string reportCode)
        {
            return $@"
        <div class='report-meta'>
            <div class='meta-item'>
                <span class='meta-label'>Generated By</span>
                <span class='meta-value'>{generatedBy}</span>
            </div>
            <div class='meta-item'>
                <span class='meta-label'>Date generated</span>
                <span class='meta-value'>{DateTime.Now:MMMM dd, yyyy HH:mm}</span>
            </div>
            <div class='meta-item'>
                <span class='meta-label'>Report ID</span>
                <span class='meta-value'>{reportCode}</span>
            </div>
        </div>";
        }

        private string GetPageFooter()
        {
            return $@"
        <div class='report-footer'>
            <p><strong>Recruitment Management System</strong></p>
            <p>This is a system-generated confidential document.</p>
            <p>&copy; {DateTime.Now.Year} Nanosoft Technologies. All rights reserved.</p>
        </div>";
        }

        private string GetReportFooter()
        {
            return $@"
    </div>
</body>
</html>";
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

        private string GenerateCandidatePDF(string generatedBy)
        {
            var candidates = _uow.Applicants.GetAll().ToList();
            var html = GetReportHeader("Candidates Report", "Detailed analysis of applicant profiles", "#3498db", "#2980b9");
            
            int firstPageLimit = 7;
            int otherPageLimit = 10;
            int total = candidates.Count;
            int processed = 0;
            int pageNum = 1;

            while (processed < total)
            {
                int limit = (pageNum == 1) ? firstPageLimit : otherPageLimit;
                var pageItems = candidates.Skip(processed).Take(limit).ToList();
                
                html += "<div class='report-frame'>";
                
                if (pageNum == 1)
                {
                    html += @"<div class='report-header-box'>
                                <h1>Candidates Report</h1>
                                <p>Detailed analysis of applicant profiles</p>
                            </div>";
                    html += GetReportMeta(generatedBy, "RPT-CAND-" + DateTime.Now.ToString("yyyyMMdd"));
                    html += $@"
                        <div class='stats-grid'>
                            <div class='stat-card-cell'>
                                <span class='stat-value'>{total}</span>
                                <span class='stat-label'>Total Candidates</span>
                            </div>
                            <div class='stat-card-cell'>
                                <span class='stat-value'>{candidates.Count(c => !string.IsNullOrEmpty(c.Email))}</span>
                                <span class='stat-label'>With Email</span>
                            </div>
                        </div>";
                }
                else
                {
                    html += $@"<div class='pagination-info' style='padding: 20px 40px 0 0;'>Page {pageNum} (Continued)</div>";
                }

                html += @"<div style='padding: 0 40px 40px 40px;'>
                            <table class='report-table'>
                                <thead>
                                    <tr>
                                        <th>ID</th>
                                        <th>Full Name</th>
                                        <th>Email</th>
                                        <th>Phone</th>
                                    </tr>
                                </thead>
                                <tbody>";

                foreach (var c in pageItems)
                {
                    html += $@"
                    <tr>
                        <td><strong>#{c.Id}</strong></td>
                        <td>{c.FullName}</td>
                        <td>{c.Email}</td>
                        <td>{c.Phone}</td>
                    </tr>";
                }

                html += "</tbody></table>" + GetPageFooter() + "</div></div>";
                
                processed += limit;
                pageNum++;
                
                if (processed < total)
                {
                    html += "<div class='page-break'></div>";
                }
            }

            html += GetReportFooter();
            return html;
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

        private string GenerateApplicationPDF(string generatedBy)
        {
            var applications = _uow.Applications.GetAll(a => a.Applicant, a => a.Position).ToList();
            var html = GetReportHeader("Applications Report", "Track job application statuses and performance", "#e74c3c", "#c0392b");
            
            int firstPageLimit = 7;
            int otherPageLimit = 10;
            int total = applications.Count;
            int processed = 0;
            int pageNum = 1;

            while (processed < total)
            {
                int limit = (pageNum == 1) ? firstPageLimit : otherPageLimit;
                var pageItems = applications.Skip(processed).Take(limit).ToList();
                
                html += "<div class='report-frame'>";
                
                if (pageNum == 1)
                {
                    html += @"<div class='report-header-box' style='background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%);'>
                                <h1>Applications Report</h1>
                                <p>Track job application statuses and performance</p>
                            </div>";
                    html += GetReportMeta(generatedBy, "RPT-APP-" + DateTime.Now.ToString("yyyyMMdd"));
                    html += $@"
                        <div class='stats-grid'>
                            <div class='stat-card-cell'>
                                <span class='stat-value' style='color:#e74c3c'>{total}</span>
                                <span class='stat-label'>Total Apps</span>
                            </div>
                            <div class='stat-card-cell'>
                                <span class='stat-value' style='color:#e74c3c'>{applications.Count(a => a.Status == "Approved")}</span>
                                <span class='stat-label'>Approved</span>
                            </div>
                            <div class='stat-card-cell'>
                                <span class='stat-value' style='color:#e74c3c'>{applications.Count(a => a.Status == "Pending")}</span>
                                <span class='stat-label'>Pending</span>
                            </div>
                        </div>";
                }
                else
                {
                    html += $@"<div class='pagination-info' style='padding: 20px 40px 0 0;'>Page {pageNum} (Continued)</div>";
                }

                html += @"<div style='padding: 0 40px 40px 40px;'>
                            <table class='report-table'>
                                <thead>
                                    <tr style='background-color:#e74c3c'>
                                        <th>ID</th>
                                        <th>Applicant</th>
                                        <th>Position</th>
                                        <th>Status</th>
                                        <th>Date</th>
                                        <th>Score</th>
                                    </tr>
                                </thead>
                                <tbody>";

                foreach (var a in pageItems)
                {
                    var badge = "badge-warning";
                    if (a.Status == "Approved") badge = "badge-success";
                    else if (a.Status == "Rejected") badge = "badge-danger";

                    html += $@"
                    <tr>
                        <td><strong>#{a.Id}</strong></td>
                        <td>{(a.Applicant != null ? a.Applicant.FullName : "N/A")}</td>
                        <td>{(a.Position != null ? a.Position.Title : "N/A")}</td>
                        <td><span class='status-badge {badge}'>{a.Status}</span></td>
                        <td>{a.AppliedOn:yyyy-MM-dd}</td>
                        <td>{a.Score}</td>
                    </tr>";
                }

                html += "</tbody></table>" + GetPageFooter() + "</div></div>";
                
                processed += limit;
                pageNum++;
                
                if (processed < total)
                {
                    html += "<div class='page-break'></div>";
                }
            }

            html += GetReportFooter();
            return html;
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

        private string GenerateInterviewPDF(string generatedBy)
        {
            var interviews = _uow.Interviews.GetAll().ToList();
            var html = GetReportHeader("Interviews Report", "Scheduled candidate assessments and feedback", "#9b59b6", "#8e44ad");
            
            int firstPageLimit = 7;
            int otherPageLimit = 10;
            int total = interviews.Count;
            int processed = 0;
            int pageNum = 1;

            while (processed < total)
            {
                int limit = (pageNum == 1) ? firstPageLimit : otherPageLimit;
                var pageItems = interviews.Skip(processed).Take(limit).ToList();
                
                html += "<div class='report-frame'>";
                
                if (pageNum == 1)
                {
                    html += @"<div class='report-header-box' style='background: linear-gradient(135deg, #9b59b6 0%, #8e44ad 100%);'>
                                <h1>Interviews Report</h1>
                                <p>Scheduled candidate assessments and feedback</p>
                            </div>";
                    html += GetReportMeta(generatedBy, "RPT-INT-" + DateTime.Now.ToString("yyyyMMdd"));
                    html += $@"
                        <div class='stats-grid'>
                            <div class='stat-card-cell'>
                                <span class='stat-value' style='color:#9b59b6'>{total}</span>
                                <span class='stat-label'>Total Interviews</span>
                            </div>
                            <div class='stat-card-cell'>
                                <span class='stat-value' style='color:#9b59b6'>{interviews.Count(i => i.ScheduledAt > DateTime.Now)}</span>
                                <span class='stat-label'>Upcoming</span>
                            </div>
                        </div>";
                }
                else
                {
                    html += $@"<div class='pagination-info' style='padding: 20px 40px 0 0;'>Page {pageNum} (Continued)</div>";
                }

                html += @"<div style='padding: 0 40px 40px 40px;'>
                            <table class='report-table'>
                                <thead>
                                    <tr style='background-color:#9b59b6'>
                                        <th>ID</th>
                                        <th>App ID</th>
                                        <th>Scheduled Date</th>
                                        <th>Mode</th>
                                        <th>Notes</th>
                                    </tr>
                                </thead>
                                <tbody>";

                foreach (var i in pageItems)
                {
                    html += $@"
                    <tr>
                        <td><strong>#{i.Id}</strong></td>
                        <td>#{i.ApplicationId}</td>
                        <td>{i.ScheduledAt:yyyy-MM-dd HH:mm}</td>
                        <td>{i.Mode}</td>
                        <td>{i.Notes}</td>
                    </tr>";
                }

                html += "</tbody></table>" + GetPageFooter() + "</div></div>";
                
                processed += limit;
                pageNum++;
                
                if (processed < total)
                {
                    html += "<div class='page-break'></div>";
                }
            }

            html += GetReportFooter();
            return html;
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

        private string GenerateDepartmentPDF(string generatedBy)
        {
            var departments = _uow.Departments.GetAll(d => d.Positions).ToList();
            var html = GetReportHeader("Departments Report", "Organizational structure and allocation", "#1abc9c", "#16a085");
            
            int firstPageLimit = 7;
            int otherPageLimit = 10;
            int total = departments.Count;
            int processed = 0;
            int pageNum = 1;

            while (processed < total)
            {
                int limit = (pageNum == 1) ? firstPageLimit : otherPageLimit;
                var pageItems = departments.Skip(processed).Take(limit).ToList();
                
                html += "<div class='report-frame'>";
                
                if (pageNum == 1)
                {
                    html += @"<div class='report-header-box' style='background: linear-gradient(135deg, #1abc9c 0%, #16a085 100%);'>
                                <h1>Departments Report</h1>
                                <p>Organizational structure and allocation</p>
                            </div>";
                    html += GetReportMeta(generatedBy, "RPT-DEPT-" + DateTime.Now.ToString("yyyyMMdd"));
                }
                else
                {
                    html += $@"<div class='pagination-info' style='padding: 20px 40px 0 0;'>Page {pageNum} (Continued)</div>";
                }

                html += @"<div style='padding: 0 40px 40px 40px;'>
                            <table class='report-table'>
                                <thead>
                                    <tr style='background-color:#1abc9c'>
                                        <th>ID</th>
                                        <th>Department Name</th>
                                        <th>Description</th>
                                        <th>Open Positions</th>
                                    </tr>
                                </thead>
                                <tbody>";

                foreach (var d in pageItems)
                {
                    html += $@"
                    <tr>
                        <td><strong>#{d.Id}</strong></td>
                        <td>{d.Name}</td>
                        <td>{d.Description}</td>
                        <td>{d.Positions?.Count ?? 0}</td>
                    </tr>";
                }

                html += "</tbody></table>" + GetPageFooter() + "</div></div>";
                
                processed += limit;
                pageNum++;
                
                if (processed < total)
                {
                    html += "<div class='page-break'></div>";
                }
            }

            html += GetReportFooter();
            return html;
        }

        private void GeneratePositionCSV(List<Position> positions, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,Title,Department,SalaryMin,SalaryMax,Currency,Location,ApplicationsCount");
                foreach (var position in positions)
                {
                    writer.WriteLine($"{position.Id},{position.Title},{position.Department?.Name},{position.SalaryMin},{position.SalaryMax},{position.Currency},{position.Location},{position.Applications?.Count ?? 0}");
                }
            }
        }

        private string GeneratePositionPDF(string generatedBy)
        {
            var positions = _uow.Positions.GetAll(p => p.Department).ToList();
            var html = GetReportHeader("Positions Report", "Job vacancy listings and budget overview", "#2ecc71", "#27ae60");
            
            int firstPageLimit = 7;
            int otherPageLimit = 10;
            int total = positions.Count;
            int processed = 0;
            int pageNum = 1;

            while (processed < total)
            {
                int limit = (pageNum == 1) ? firstPageLimit : otherPageLimit;
                var pageItems = positions.Skip(processed).Take(limit).ToList();
                
                html += "<div class='report-frame'>";
                
                if (pageNum == 1)
                {
                    html += @"<div class='report-header-box' style='background: linear-gradient(135deg, #2ecc71 0%, #27ae60 100%);'>
                                <h1>Positions Report</h1>
                                <p>Job vacancy listings and budget overview</p>
                            </div>";
                    html += GetReportMeta(generatedBy, "RPT-POS-" + DateTime.Now.ToString("yyyyMMdd"));
                }
                else
                {
                    html += $@"<div class='pagination-info' style='padding: 20px 40px 0 0;'>Page {pageNum} (Continued)</div>";
                }

                html += @"<div style='padding: 0 40px 40px 40px;'>
                            <table class='report-table'>
                                <thead>
                                    <tr style='background-color:#2ecc71'>
                                        <th>ID</th>
                                        <th>Job Title</th>
                                        <th>Department</th>
                                        <th>Salary Range</th>
                                        <th>Location</th>
                                    </tr>
                                </thead>
                                <tbody>";

                foreach (var p in pageItems)
                {
                    html += $@"
                    <tr>
                        <td><strong>#{p.Id}</strong></td>
                        <td>{p.Title}</td>
                        <td>{(p.Department != null ? p.Department.Name : "N/A")}</td>
                        <td>{(p.Currency ?? "KES")} {p.SalaryMin:N0} - {p.SalaryMax:N0}</td>
                        <td>{p.Location}</td>
                    </tr>";
                }

                html += "</tbody></table>" + GetPageFooter() + "</div></div>";
                
                processed += limit;
                pageNum++;
                
                if (processed < total)
                {
                    html += "<div class='page-break'></div>";
                }
            }

            html += GetReportFooter();
            return html;
        }

        private void GenerateSecurityCSV(List<AuditLog> auditLogs, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ID,Username,Action,Controller,Timestamp,IPAddress");
                foreach (var log in auditLogs)
                {
                    writer.WriteLine($"{log.Id},{log.Username},{log.Action},{log.Controller},{log.Timestamp:yyyy-MM-dd HH:mm},{log.IPAddress}");
                }
            }
        }

        private string GenerateSecurityPDF(string generatedBy)
        {
            var auditLogs = _uow.AuditLogs.GetAll().OrderByDescending(l => l.Timestamp).Take(200).ToList();
            var html = GetReportHeader("Security & Audit Report", "Log of critical system actions and access", "#34495e", "#2c3e50");
            
            int firstPageLimit = 7;
            int otherPageLimit = 10;
            int total = auditLogs.Count;
            int processed = 0;
            int pageNum = 1;

            while (processed < total)
            {
                int limit = (pageNum == 1) ? firstPageLimit : otherPageLimit;
                var pageItems = auditLogs.Skip(processed).Take(limit).ToList();
                
                html += "<div class='report-frame'>";
                
                if (pageNum == 1)
                {
                    html += @"<div class='report-header-box' style='background: linear-gradient(135deg, #34495e 0%, #2c3e50 100%);'>
                                <h1>Security Audit Report</h1>
                                <p>Log of critical system actions and access</p>
                            </div>";
                    html += GetReportMeta(generatedBy, "RPT-SEC-" + DateTime.Now.ToString("yyyyMMdd"));
                }
                else
                {
                    html += $@"<div class='pagination-info' style='padding: 20px 40px 0 0;'>Page {pageNum} (Continued)</div>";
                }

                html += @"<div style='padding: 0 40px 40px 40px;'>
                            <table class='report-table'>
                                <thead>
                                    <tr style='background-color:#34495e'>
                                        <th>Timestamp</th>
                                        <th>User</th>
                                        <th>Action</th>
                                        <th>Module</th>
                                        <th>IP Address</th>
                                    </tr>
                                </thead>
                                <tbody>";

                foreach (var l in pageItems)
                {
                    html += $@"
                    <tr>
                        <td>{l.Timestamp:yyyy-MM-dd HH:mm}</td>
                        <td><strong>{l.Username}</strong></td>
                        <td>{l.Action}</td>
                        <td>{l.Controller}</td>
                        <td>{l.IPAddress}</td>
                    </tr>";
                }

                html += "</tbody></table>" + GetPageFooter() + "</div></div>";
                
                processed += limit;
                pageNum++;
                
                if (processed < total)
                {
                    html += "<div class='page-break'></div>";
                }
            }

            html += GetReportFooter();
            return html;
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
