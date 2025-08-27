using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_TENANT_SERVICE.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Category Product
            CreateMap<Product, ProductRequest>().ReverseMap();
            CreateMap<Product, ProductGetRequest>().ReverseMap();
            // Mapping Product -> ProductResponse
            CreateMap<Product, ProductResponse>()
                .ForMember(dest => dest.PromotionValue,
                    opt => opt.MapFrom(src =>
                        src.PromotionProducts.Select(pp => pp.Promotion.Value).FirstOrDefault()
                    ))
                .ForMember(dest => dest.PromotionType,
                    opt => opt.MapFrom(src =>
                        src.PromotionProducts.Select(pp => pp.Promotion.Type).FirstOrDefault()
                    ))
                .ReverseMap();
        }
    }
}
