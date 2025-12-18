using System.Data.Entity;
using HR.Web.Models;
using Oracle.ManagedDataAccess.EntityFramework;

namespace HR.Web.Data
{
    //[DbConfigurationType(typeof(OracleEFConfiguration))] // Commented for local SQL testing; re-enable for Oracle
    public class HrContext : DbContext
    {
        public HrContext() : base("name=HrContext")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Onboarding> Onboardings { get; set; }

        public DbSet<Question> Questions { get; set; }
        public DbSet<PositionQuestion> PositionQuestions { get; set; }
        public DbSet<ApplicationAnswer> ApplicationAnswers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.HasDefaultSchema("HR_APP"); // Commented for local SQL testing; use default schema (dbo)

            modelBuilder.Entity<Application>()
                .HasRequired(a => a.Applicant)
                .WithMany(ap => ap.Applications)
                .HasForeignKey(a => a.ApplicantId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Application>()
                .HasRequired(a => a.Position)
                .WithMany(p => p.Applications)
                .HasForeignKey(a => a.PositionId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Interview>()
                .HasRequired(i => i.Application)
                .WithMany()
                .HasForeignKey(i => i.ApplicationId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Interview>()
                .HasRequired(i => i.Interviewer)
                .WithMany(u => u.Interviews)
                .HasForeignKey(i => i.InterviewerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Onboarding>()
                .HasRequired(o => o.Application)
                .WithMany()
                .HasForeignKey(o => o.ApplicationId)
                .WillCascadeOnDelete(false);
        }
    }
}


