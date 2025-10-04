using ASA_PLATFORM_REPO.DBContext;
using ASA_PLATFORM_REPO.Models;
using EDUConnect_Repositories.Basic;
using Microsoft.EntityFrameworkCore;


namespace ASA_PLATFORM_REPO.Repository
{
    public class ProductRepo : GenericRepository<Product>
    {
        public ProductRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }
        public IQueryable<Product> GetFiltered(Product filter)
        {
            var query = _context.Products
                                .Include(p => p.PromotionProducts)
                                    .ThenInclude(pp => pp.Promotion)
                                    .Include(p => p.Features)
                                .AsQueryable();

            if (filter.ProductId > 0)
                query = query.Where(c => c.ProductId == filter.ProductId);
            if (!string.IsNullOrEmpty(filter.ProductName))
                query = query.Where(c => c.ProductName.Contains(filter.ProductName));
            if (filter.Price.HasValue)
                query = query.Where(p => p.Price <= filter.Price.Value);
            if (filter.RequestLimit.HasValue)
                query = query.Where(p => p.RequestLimit <= filter.RequestLimit.Value);

            if (filter.AccountLimit.HasValue)
                query = query.Where(p => p.AccountLimit <= filter.AccountLimit.Value);

            if (filter.CreatedAt.HasValue)
                query = query.Where(p => p.CreatedAt <= filter.CreatedAt.Value);

            if (filter.UpdatedAt.HasValue)
                query = query.Where(p => p.UpdatedAt <= filter.UpdatedAt.Value);

            if (filter.Discount.HasValue)
                query = query.Where(p => p.Discount <= filter.Discount.Value);

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);

            return query.OrderBy(c => c.ProductId);
        }

        public async Task<List<long>> GetInvalidFeatureIdsAsync(IEnumerable<long> featureIds)
        {
            if (featureIds == null || !featureIds.Any())
                return new List<long>();

            // Lấy toàn bộ Features có trong DB
            var validIds = _context.Features
                .Where(p => featureIds.Contains(p.FeatureId))
                .Select(p => p.FeatureId)
                .ToList();

            // Những id nào ko nằm trong validIds thì là invalid
            var invalidIds = featureIds.Except(validIds).ToList();

            return invalidIds;
        }

        public async Task AttachFeaturesAsync(Product product, IEnumerable<long> featureIds)
        {
            var features = await _context.Features
                .Where(f => featureIds.Contains(f.FeatureId))
                .ToListAsync();

            foreach (var feature in features)
            {
                product.Features.Add(feature);
            }
        }

        public async Task<Product> GetByIdAsync(long id)
        {
            return await _context.Products
                                 .Include(p => p.PromotionProducts)
                                    .ThenInclude(pp => pp.Promotion)
                                 .Include(p => p.Features)
                                 .FirstOrDefaultAsync(p => p.ProductId == id);
        }


        public async Task<int> UpdateAsync(Product product, IEnumerable<long> featureIds = null)
        {
            var existing = await _context.Products
                .Include(p => p.Features)
                .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

            if (existing == null) return 0;

            // Update scalar fields
            _context.Entry(existing).CurrentValues.SetValues(product);

            // Update quan hệ Features
            existing.Features.Clear();
            if (featureIds != null && featureIds.Any())
            {
                var features = await _context.Features
                    .Where(f => featureIds.Contains(f.FeatureId))
                    .ToListAsync();

                foreach (var feature in features)
                {
                    existing.Features.Add(feature);
                }
            }

            return await _context.SaveChangesAsync();
        }


    }
}
