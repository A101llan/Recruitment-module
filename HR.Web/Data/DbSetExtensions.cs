using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace HR.Web.Data
{
    /// <summary>
    /// EF5 on .NET 4.0 lacks DbSet.RemoveRange (added in EF6).
    /// </summary>
    public static class DbSetExtensions
    {
        public static void RemoveRange<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (dbSet == null || entities == null)
            {
                return;
            }

            foreach (var entity in entities.ToList())
            {
                dbSet.Remove(entity);
            }
        }
    }
}
