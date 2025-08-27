using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class PromotionProductService : IPromotionProductService
    {
        private readonly PromotionProductRepo _promotionProductRepo;
        private readonly IMapper _mapper;
        public PromotionProductService(PromotionProductRepo promotionProductRepo, IMapper mapper)
        {
            _promotionProductRepo = promotionProductRepo;
            _mapper = mapper;
        }
    }
}
