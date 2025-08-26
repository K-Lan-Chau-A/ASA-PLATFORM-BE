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
    public class ShopService : IShopService
    {
        private readonly ShopRepo _shopRepo;
        private readonly IMapper _mapper;
        public ShopService(ShopRepo shopRepo, IMapper mapper)
        {
            _shopRepo = shopRepo;
            _mapper = mapper;
        }
    }
}
