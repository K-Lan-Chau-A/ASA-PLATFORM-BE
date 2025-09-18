using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_REPO.Repository
{
    public class LogActivityRepo : GenericRepository<LogActivity>
    {
        public LogActivityRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }
        public IQueryable<LogActivity> GetFiltered(LogActivity filter)
        {
            var query = _context.LogActivities.AsQueryable();

            if (filter.LogActivityId > 0)
                query = query.Where(c => c.LogActivityId == filter.LogActivityId);
            if (!string.IsNullOrEmpty(filter.Content))
                query = query.Where(c => c.Content.Contains(filter.Content));
            if (filter.Type.HasValue)
                query = query.Where(p => p.Type == filter.Type);
            if (filter.CreatedAt.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.CreatedAt);

            if (filter.UserId > 0)
                query = query.Where(p => p.UserId == filter.UserId);

            return query.OrderBy(c => c.LogActivityId);
        }
    }
}
