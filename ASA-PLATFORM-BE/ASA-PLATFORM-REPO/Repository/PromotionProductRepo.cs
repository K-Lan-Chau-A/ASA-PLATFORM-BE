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
    public class PromotionProductRepo : GenericRepository<PromotionProduct>
    {
        public PromotionProductRepo(ASAPLATFORMDBContext context) : base(context)
        {
        }

        // Get all PromotionProduct entries by PromotionId
        public async Task<List<PromotionProduct>> GetByPromotionIdAsync(long promotionId)
        {
            return await _context.PromotionProducts
                .Where(pp => pp.PromotionId == promotionId)
                .ToListAsync();
        }

        public IQueryable<PromotionProduct> GetFiltered(PromotionProduct filter)
        {
            var query = _context.PromotionProducts.Include(pp => pp.Product).Include(pp => pp.Promotion).AsQueryable();
            if (filter.PromotionProductId > 0)
                query = query.Where(pp => pp.PromotionProductId == filter.PromotionProductId);
            if (filter.PromotionId > 0)
                query = query.Where(pp => pp.PromotionId == filter.PromotionId);
            if (filter.ProductId > 0)
                query = query.Where(pp => pp.ProductId == filter.ProductId);
            return query.OrderBy(pp => pp.PromotionProductId);  
        }
    }
}
