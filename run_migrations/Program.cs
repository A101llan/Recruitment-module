using System;
using System.Data.Entity.Migrations;
using System.Reflection;

class Program
{
    static int Main()
    {
        try
        {
            Console.WriteLine("Starting EF migrations...");
            var asm = Assembly.LoadFrom("c:\\Users\\allan\\Documents\\Examples\\HR\\HR.Web\\bin\\HR.Web.dll");
            var cfgType = asm.GetType("HR.Web.Migrations.Configuration");
            var cfg = (DbMigrationsConfiguration)Activator.CreateInstance(cfgType, true);
            var migrator = new DbMigrator(cfg);
            migrator.Update();
            Console.WriteLine("Migrations applied successfully.");
            TrySeedDemo();
            return 0;
        }
        catch (System.Data.SqlClient.SqlException sqlEx)
        {
            Console.Error.WriteLine("Migration SQL error: " + sqlEx.Message);
            if (sqlEx.Message.IndexOf("already an object named", StringComparison.OrdinalIgnoreCase) >= 0 || sqlEx.Number == 2714)
            {
                try
                {
                    Console.WriteLine("Attempting to drop existing database HR_Local and retry...");
                    var masterConn = new System.Data.SqlClient.SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;");
                    masterConn.Open();
                    var dropCmd = masterConn.CreateCommand();
                    dropCmd.CommandText = "ALTER DATABASE [HR_Local] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [HR_Local];";
                    dropCmd.ExecuteNonQuery();
                    masterConn.Close();

                    var asm = Assembly.LoadFrom("c:\\Users\\allan\\Documents\\Examples\\HR\\HR.Web\\bin\\HR.Web.dll");
                    var cfgType = asm.GetType("HR.Web.Migrations.Configuration");
                    var cfg = (DbMigrationsConfiguration)Activator.CreateInstance(cfgType, true);
                    var migrator = new DbMigrator(cfg);
                    migrator.Update();
                    Console.WriteLine("Migrations applied successfully after dropping DB.");
                    TrySeedDemo();
                    return 0;
                }
                catch (Exception ex2)
                {
                    Console.Error.WriteLine("Retry failed: " + ex2);
                    return 1;
                }
            }
            Console.Error.WriteLine("Migration failed: " + sqlEx);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Migration failed: " + ex);
            return 1;
        }
    }

    static void TrySeedDemo()
{
    try
    {
        Console.WriteLine("Running demo seed (reflection)...");
        var asm = Assembly.LoadFrom("c:\\Users\\allan\\Documents\\Examples\\HR\\HR.Web\\bin\\HR.Web.dll");
        var uowType = asm.GetType("HR.Web.Data.UnitOfWork");
        var uow = Activator.CreateInstance(uowType);
        try
        {
            Func<string, object> getRepo = (name) => uowType.GetProperty(name).GetValue(uow);
            Func<object, System.Collections.IEnumerable> getAll = (repo) => {
                // Prefer accessing the internal DbSet<T> field named "_set" to avoid param mismatch on GetAll
                var field = repo.GetType().GetField("_set", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var set = field.GetValue(repo) as System.Collections.IEnumerable;
                    return set;
                }
                // Fallback to calling GetAll via reflection (handle params signature)
                var mi = repo.GetType().GetMethod("GetAll");
                var parameters = mi.GetParameters();
                object res;
                if (parameters.Length == 0)
                {
                    res = mi.Invoke(repo, null);
                }
                else
                {
                    var paramType = parameters[0].ParameterType;
                    var elemType = paramType.GetElementType();
                    var emptyArray = Array.CreateInstance(elemType, 0);
                    res = mi.Invoke(repo, new object[] { emptyArray });
                }
                return (System.Collections.IEnumerable)res;
            };
            Action<object, object> addEntity = (repo, entity) => repo.GetType().GetMethod("Add").Invoke(repo, new[] { entity });
            var completeMi = uowType.GetMethod("Complete");
            Action commit = () => { completeMi.Invoke(uow, null); };

            // Types
            var deptType = asm.GetType("HR.Web.Models.Department");
            var userType = asm.GetType("HR.Web.Models.User");
            var positionType = asm.GetType("HR.Web.Models.Position");
            var applicantType = asm.GetType("HR.Web.Models.Applicant");
            var applicationType = asm.GetType("HR.Web.Models.Application");
            var interviewType = asm.GetType("HR.Web.Models.Interview");
            var onboardingType = asm.GetType("HR.Web.Models.Onboarding");

            var depRepo = getRepo("Departments");
            var deps = getAll(depRepo).GetEnumerator();
            var hasDeps = deps.MoveNext();
            if (!hasDeps)
            {
                var d1 = Activator.CreateInstance(deptType);
                deptType.GetProperty("Name").SetValue(d1, "Engineering");
                addEntity(depRepo, d1);
                var d2 = Activator.CreateInstance(deptType);
                deptType.GetProperty("Name").SetValue(d2, "HR");
                addEntity(depRepo, d2);
            }

            var userRepo = getRepo("Users");
            var usersEnum = getAll(userRepo).GetEnumerator();
            var hasUsers = usersEnum.MoveNext();
            if (!hasUsers)
            {
                var u1 = Activator.CreateInstance(userType);
                userType.GetProperty("UserName").SetValue(u1, "admin");
                userType.GetProperty("Email").SetValue(u1, "admin@test.com");
                userType.GetProperty("Role").SetValue(u1, "Admin");
                addEntity(userRepo, u1);

                var u2 = Activator.CreateInstance(userType);
                userType.GetProperty("UserName").SetValue(u2, "hr");
                userType.GetProperty("Email").SetValue(u2, "hr@test.com");
                userType.GetProperty("Role").SetValue(u2, "HR");
                addEntity(userRepo, u2);

                var u3 = Activator.CreateInstance(userType);
                userType.GetProperty("UserName").SetValue(u3, "client");
                userType.GetProperty("Email").SetValue(u3, "client@test.com");
                userType.GetProperty("Role").SetValue(u3, "Client");
                addEntity(userRepo, u3);
            }

            var posRepo = getRepo("Positions");
            var posEnum = getAll(posRepo).GetEnumerator();
            var hasPos = posEnum.MoveNext();
            if (!hasPos)
            {
                // get first department id
                var depList = getAll(depRepo).GetEnumerator();
                depList.MoveNext();
                var firstDep = depList.Current;
                var firstDepId = (int) deptType.GetProperty("Id").GetValue(firstDep);

                var p1 = Activator.CreateInstance(positionType);
                positionType.GetProperty("Title").SetValue(p1, "Software Engineer");
                positionType.GetProperty("Description").SetValue(p1, "Build and maintain web apps.");
                positionType.GetProperty("SalaryMin").SetValue(p1, 70000);
                positionType.GetProperty("SalaryMax").SetValue(p1, 110000);
                positionType.GetProperty("DepartmentId").SetValue(p1, firstDepId);
                positionType.GetProperty("IsOpen").SetValue(p1, true);
                positionType.GetProperty("PostedOn").SetValue(p1, DateTime.UtcNow.AddDays(-7));
                addEntity(posRepo, p1);

                // HR position if HR dept exists
                var hrDep = default(object);
                var depEnum2 = getAll(depRepo).GetEnumerator();
                while (depEnum2.MoveNext())
                {
                    var dd = depEnum2.Current;
                    var name = (string)deptType.GetProperty("Name").GetValue(dd);
                    if (name == "HR") { hrDep = dd; break; }
                }
                if (hrDep != null)
                {
                    var p2 = Activator.CreateInstance(positionType);
                    positionType.GetProperty("Title").SetValue(p2, "HR Generalist");
                    positionType.GetProperty("Description").SetValue(p2, "Support hiring and onboarding.");
                    positionType.GetProperty("SalaryMin").SetValue(p2, 50000);
                    positionType.GetProperty("SalaryMax").SetValue(p2, 80000);
                    positionType.GetProperty("DepartmentId").SetValue(p2, (int)deptType.GetProperty("Id").GetValue(hrDep));
                    positionType.GetProperty("IsOpen").SetValue(p2, true);
                    positionType.GetProperty("PostedOn").SetValue(p2, DateTime.UtcNow.AddDays(-3));
                    addEntity(posRepo, p2);
                }
            }

            var applicantRepo = getRepo("Applicants");
            var applicantEnum = getAll(applicantRepo).GetEnumerator();
            var hasApplicants = applicantEnum.MoveNext();
            if (!hasApplicants)
            {
                var a1 = Activator.CreateInstance(applicantType);
                applicantType.GetProperty("FullName").SetValue(a1, "Alice Johnson");
                applicantType.GetProperty("Email").SetValue(a1, "alice@applicant.com");
                applicantType.GetProperty("Phone").SetValue(a1, "555-1234");
                addEntity(applicantRepo, a1);

                var a2 = Activator.CreateInstance(applicantType);
                applicantType.GetProperty("FullName").SetValue(a2, "Bob Smith");
                applicantType.GetProperty("Email").SetValue(a2, "bob@applicant.com");
                applicantType.GetProperty("Phone").SetValue(a2, "555-5678");
                addEntity(applicantRepo, a2);
            }

            // Applications + related
            var appRepo = getRepo("Applications");
            var appEnum2 = getAll(appRepo).GetEnumerator();
            var hasApps = appEnum2.MoveNext();
            if (!hasApps)
            {
                // get first position and applicant
                var posEnum2 = getAll(posRepo).GetEnumerator(); posEnum2.MoveNext(); var posObj = posEnum2.Current;
                var applicantEnum2 = getAll(applicantRepo).GetEnumerator(); applicantEnum2.MoveNext(); var applicantObj = applicantEnum2.Current;

                var newApp = Activator.CreateInstance(applicationType);
                applicationType.GetProperty("ApplicantId").SetValue(newApp, (int)applicantType.GetProperty("Id").GetValue(applicantObj));
                applicationType.GetProperty("PositionId").SetValue(newApp, (int)positionType.GetProperty("Id").GetValue(posObj));
                applicationType.GetProperty("Status").SetValue(newApp, "Screening");
                applicationType.GetProperty("AppliedOn").SetValue(newApp, DateTime.UtcNow.AddDays(-2));
                applicationType.GetProperty("ResumePath").SetValue(newApp, null);
                addEntity(appRepo, newApp);
                commit();

                // reload saved app
                var savedAppEnum = getAll(appRepo).GetEnumerator(); savedAppEnum.MoveNext(); var savedApp = savedAppEnum.Current;
                // find an HR user
                var userEnum2 = getAll(userRepo).GetEnumerator(); object interviewer = null;
                while (userEnum2.MoveNext()) { var u = userEnum2.Current; var role = (string)userType.GetProperty("Role").GetValue(u); if (role == "HR") { interviewer = u; break; } }
                if (interviewer != null)
                {
                    var interviewRepo = getRepo("Interviews");
                    var onboardingRepo = getRepo("Onboardings");
                    var i1 = Activator.CreateInstance(interviewType);
                    interviewType.GetProperty("ApplicationId").SetValue(i1, (int)applicationType.GetProperty("Id").GetValue(savedApp));
                    interviewType.GetProperty("InterviewerId").SetValue(i1, (int)userType.GetProperty("Id").GetValue(interviewer));
                    interviewType.GetProperty("ScheduledAt").SetValue(i1, DateTime.UtcNow.AddDays(1));
                    interviewType.GetProperty("Mode").SetValue(i1, "Remote");
                    interviewType.GetProperty("Notes").SetValue(i1, "Initial HR screen");
                    addEntity(interviewRepo, i1);

                    var o1 = Activator.CreateInstance(onboardingType);
                    onboardingType.GetProperty("ApplicationId").SetValue(o1, (int)applicationType.GetProperty("Id").GetValue(savedApp));
                    onboardingType.GetProperty("StartDate").SetValue(o1, DateTime.UtcNow.AddDays(14));
                    onboardingType.GetProperty("Tasks").SetValue(o1, "Complete paperwork; provision laptop");
                    onboardingType.GetProperty("Status").SetValue(o1, "Pending");
                    addEntity(onboardingRepo, o1);
                }
            }

            commit();
        }
        finally
        {
            var dispose = uow as IDisposable;
            if (dispose != null) dispose.Dispose();
        }
        Console.WriteLine("Demo seed complete (reflection).");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Seed failed: " + ex);
    }
}
}
