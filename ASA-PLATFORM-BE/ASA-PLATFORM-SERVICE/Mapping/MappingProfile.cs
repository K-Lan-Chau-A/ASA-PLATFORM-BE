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
                .ForMember(dest => dest.Features,
                    opt => opt.MapFrom(src => src.Features))
                .ReverseMap()
                .ForMember(dest => dest.Features, opt => opt.Ignore());

            //Mapping Shop
            CreateMap<Shop, ShopRequest>().ReverseMap();
            CreateMap<Shop, ShopGetRequest>().ReverseMap();
            CreateMap<Shop, ShopResponse>().ReverseMap();

            //Mapping User
            CreateMap<User, UserRequest>().ReverseMap();
            CreateMap<User, UserGetRequest>().ReverseMap();
            CreateMap<User, UserResponse>().ReverseMap();
            CreateMap<User, LoginResponse > ().ReverseMap();
            CreateMap<User, CurrentAccount>().ReverseMap();

            //Mapping LogActivity
            CreateMap<LogActivity, LogActivityRequest>().ReverseMap();
            CreateMap<LogActivity, LogActivityGetRequest>().ReverseMap();
            CreateMap<LogActivity, LogActivityResponse>().ReverseMap();

            //Mapping Order
            CreateMap<Order, OrderRequest>().ReverseMap();
            CreateMap<Order, OrderGetRequest>().ReverseMap();
            CreateMap<Order, OrderResponse>().ReverseMap();

            //Mapping Promotion
            CreateMap<Promotion, PromotionRequest>().ReverseMap();
            CreateMap<Promotion, PromotionGetRequest>().ReverseMap();
            CreateMap<Promotion, PromotionResponse>().ReverseMap();

            //Mapping PromotionProduct
            CreateMap<PromotionProduct, PromotionProductRequest>().ReverseMap();
            CreateMap<PromotionProduct, PromotionProductGetRequest>().ReverseMap();
            CreateMap<PromotionProduct, PromotionProductResponse>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product.ProductName))
                .ForMember(dest => dest.PromotionName, otp => otp.MapFrom(src => src.Promotion.PromotionName))
                .ForMember(dest => dest.Description, otp => otp.MapFrom(src => src.Promotion.Description))
                .ReverseMap();

            // Mapping Feature
            CreateMap<Feature, FeatureResponse>();

            // Mapping Report
            CreateMap<Report, ReportRequest>().ReverseMap();
            CreateMap<Report, ReportGetRequest>().ReverseMap();
            CreateMap<Report, ReportResponse>().ReverseMap();
        }
    }
}
