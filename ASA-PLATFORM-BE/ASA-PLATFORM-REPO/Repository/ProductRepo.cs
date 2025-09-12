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
    }
}
