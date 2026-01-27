using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Services
{
    public class SecurityService
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        
        // Account lockout settings
        private const int MaxFailedAttempts = 5;
        private const int LockoutDurationMinutes = 30;
        
        public bool IsAccountLocked(string username)
        {
            var recentFailedAttempts = _uow.LoginAttempts.GetAll()
                .Where(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) 
                           && !a.WasSuccessful 
                           && a.AttemptTime > DateTime.Now.AddMinutes(-LockoutDurationMinutes))
                .Count();
                
            return recentFailedAttempts >= MaxFailedAttempts;
        }
        
        public DateTime? GetLockoutEndTime(string username)
        {
            var failedAttempts = _uow.LoginAttempts.GetAll()
                .Where(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) 
                           && !a.WasSuccessful 
                           && a.AttemptTime > DateTime.Now.AddMinutes(-LockoutDurationMinutes))
                .OrderByDescending(a => a.AttemptTime)
                .Take(MaxFailedAttempts)
                .ToList();
                
            if (failedAttempts.Count >= MaxFailedAttempts)
            {
                var oldestAttempt = failedAttempts.LastOrDefault();
                return oldestAttempt?.AttemptTime.AddMinutes(LockoutDurationMinutes);
            }
            
            return null;
        }
        
        public void RecordLoginAttempt(string username, string ipAddress, bool wasSuccessful, string failureReason = null)
        {
            var attempt = new LoginAttempt
            {
                Username = username,
                IPAddress = ipAddress,
                AttemptTime = DateTime.Now,
                WasSuccessful = wasSuccessful,
                FailureReason = failureReason
            };
            
            _uow.LoginAttempts.Add(attempt);
            _uow.Complete();
        }
        
        public int GetRemainingAttempts(string username)
        {
            var recentFailedAttempts = _uow.LoginAttempts.GetAll()
                .Where(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) 
                           && !a.WasSuccessful 
                           && a.AttemptTime > DateTime.Now.AddMinutes(-LockoutDurationMinutes))
                .Count();
                
            return Math.Max(0, MaxFailedAttempts - recentFailedAttempts);
        }
        
        public void ClearFailedAttempts(string username)
        {
            var failedAttempts = _uow.LoginAttempts.GetAll()
                .Where(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) 
                           && !a.WasSuccessful)
                .ToList();
                
            foreach (var attempt in failedAttempts)
            {
                _uow.LoginAttempts.Remove(attempt);
            }
            
            _uow.Complete();
        }
    }
}
