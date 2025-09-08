using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_REPO.Repository
{
    public class PromotionRepo : GenericRepository<Promotion>
    {
        public PromotionRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }

        public IQueryable<Promotion> GetFiltered(Promotion filter)
        {
            var query = _context.Promotions.AsQueryable();
            if (filter.PromotionId > 0)
                query = query.Where(c => c.PromotionId == filter.PromotionId);
            if (!string.IsNullOrEmpty(filter.PromotionName))
                query = query.Where(c => c.PromotionName.Contains(filter.PromotionName));
            if (!string.IsNullOrEmpty(filter.Description))
                query = query.Where(c => c.Description.Contains(filter.Description));
            if (filter.StartDate.HasValue)
                query = query.Where(p => p.StartDate <= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(p => p.EndDate <= filter.EndDate.Value);
            if (filter.Value.HasValue)
                query = query.Where(p => p.Value <= filter.Value.Value);
            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(c => c.Type.Contains(filter.Type));
            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);
            if (filter.CreatedAt.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.CreatedAt.Value);
            return query.OrderBy(c => c.PromotionId);
        }
    }
}
