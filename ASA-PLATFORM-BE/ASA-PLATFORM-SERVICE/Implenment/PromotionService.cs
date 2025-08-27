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
    public class PromotionService : IPromotionService
    {
        private readonly ProductRepo productRepo;
        private readonly IMapper _mapper;
        public PromotionService(ProductRepo productRepo, IMapper mapper)
        {
            this.productRepo = productRepo;
            _mapper = mapper;
        }
    }
}
