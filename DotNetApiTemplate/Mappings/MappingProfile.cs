using AutoMapper;
using DotNetApiTemplate.Models.Data;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Request.Product;
using SupermarketERP.Models.Request.Warehouse;
using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.BranchManagerResponse;
using SupermarketERP.Models.Response.Employee;
using SupermarketERP.Models.Response.Product;
using SupermarketERP.Models.Response.Warehouse;

namespace DotNetApiTemplate.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region BRANCHES

        CreateMap<Branch, BranchResponse>()
            .ForMember(dest => dest.IsManager,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsAdmin,
                opt => opt.MapFrom(src => src.Workers));

        CreateMap<CreateBranchRequest, Branch>()
            .ForMember(dest => dest.Id,
                opt => opt.Ignore())
            .ForMember(dest => dest.Code,
                opt => opt.Ignore())
            .ForMember(dest => dest.BranchManager,
                opt => opt.Ignore())
            .ForMember(dest => dest.Workers,
                opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseId,
                opt => opt.Ignore())
            .ForMember(dest => dest.Warehouse,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,
                opt => opt.Ignore());

        CreateMap<UpdateBranchRequest, Branch>()
            .ForMember(dest => dest.Id,
                opt => opt.Ignore())
            .ForMember(dest => dest.Code,
                opt => opt.Ignore())
            .ForMember(dest => dest.BranchManager,
                opt => opt.Ignore())
            .ForMember(dest => dest.Workers,
                opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseId,
                opt => opt.Ignore())
            .ForMember(dest => dest.Warehouse,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsActive,
                opt => opt.Ignore());

        #endregion

        #region BRANCH MANAGERS

        CreateMap<BranchManager, BranchManagerResponse>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src =>
                    src.User != null
                        ? src.User.Email
                        : null))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src =>
                    src.User != null
                        ? src.User.UserName
                        : null));

        #endregion

        #region BRANCH WORKERS

        CreateMap<BranchWorker, BranchWorkerResponse>();

        #endregion

        #region WAREHOUSES

        CreateMap<CreateWarehouseRequest, Warehouse>()
            .ForMember(dest => dest.Id,
                opt => opt.Ignore())
            .ForMember(dest => dest.Code,
                opt => opt.Ignore())
            .ForMember(dest => dest.Branches,
                opt => opt.Ignore())
            .ForMember(dest => dest.Workers,
                opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseManager,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,
                opt => opt.Ignore());

        CreateMap<Warehouse, WarehouseResponse>()
            .ForMember(dest => dest.WarehouseManager,
                opt => opt.MapFrom(src => src.WarehouseManager))
            .ForMember(dest => dest.WarehouseManagerId,
                opt => opt.MapFrom(src =>
                    src.WarehouseManager != null
                        ? (Guid?)src.WarehouseManager.Id
                        : null))
            .ForMember(dest => dest.IsManager,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsAdmin,
                opt => opt.Ignore());

        #endregion

        #region WAREHOUSE MANAGERS

        CreateMap<WarehouseManager, WarehouseWorkerResponse>()
            .ForMember(dest => dest.Id,
                opt => opt.MapFrom(src => src.Id));

        #endregion

        #region WAREHOUSE WORKERS

        CreateMap<WarehouseWorker, WarehouseWorkerResponse>();

        #endregion

        #region PRODUCTS

        CreateMap<Product, ProductResponse>();
        CreateMap<List<Product>, ProductResponse>();
        CreateMap<CreateProductRequest, Product>();

        #endregion
    }
}