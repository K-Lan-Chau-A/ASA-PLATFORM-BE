using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using Microsoft.EntityFrameworkCore;

namespace ASA_PLATFORM_REPO.Repository
{
    public class FeatureRepo : GenericRepository<Feature>
    {
        public FeatureRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }

        public IQueryable<Feature> GetFiltered(Feature filter)
        {
            var query = _context.Features
                                .Include(f => f.Products)
                                .AsQueryable();

            if (filter.FeatureId > 0)
                query = query.Where(f => f.FeatureId == filter.FeatureId);
            if (!string.IsNullOrEmpty(filter.FeatureName))
                query = query.Where(f => f.FeatureName.Contains(filter.FeatureName));
            if (!string.IsNullOrEmpty(filter.Description))
                query = query.Where(f => f.Description.Contains(filter.Description));
            if (filter.IsActive.HasValue)
                query = query.Where(f => f.IsActive == filter.IsActive.Value);
            if (filter.CreatedAt.HasValue)
                query = query.Where(f => f.CreatedAt <= filter.CreatedAt.Value);
            if (filter.UpdatedAt.HasValue)
                query = query.Where(f => f.UpdatedAt <= filter.UpdatedAt.Value);

            return query.OrderBy(f => f.FeatureId);
        }

        public async Task<Feature> GetByIdAsync(long id)
        {
            return await _context.Features
                                 .Include(f => f.Products)
                                 .FirstOrDefaultAsync(f => f.FeatureId == id);
        }
    }
}
