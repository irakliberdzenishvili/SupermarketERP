using AutoMapper;
using DotNetApiTemplate.Models.Data;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Request.Inventory;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Request.Stock;
using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.BranchManagerResponse;
using SupermarketERP.Models.Response.Employee;
using SupermarketERP.Models.Response.Payments;
using SupermarketERP.Models.Response.Sales;
using SupermarketERP.Models.Response.Stock;

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
                opt => opt.Ignore());

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

        CreateMap<Branch, BranchDetailsResponse>()
    .ForMember(dest => dest.IsManager,
        opt => opt.Ignore())
    .ForMember(dest => dest.IsAdmin,
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

        CreateMap<UpdateProductRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SKU, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());

        CreateMap<ChangeProductStatusRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SKU, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.Barcode, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.Ignore());

        #endregion

        #region INVENTORY

        CreateMap<Inventory, InventoryResponse>();

        CreateMap<List<Inventory>, InventoryResponse>();

        CreateMap<CreateInventoryRequest, Inventory>();

        CreateMap<AdjustInventoryRequest, InventoryAdjustment>();

        CreateMap<List<InventoryAdjustment>, InventoryAdjustmentResponse>();

        CreateMap<InventoryAdjustment, InventoryAdjustmentResponse>();

        #endregion

        #region STOCK

        CreateMap<StockTransfer, StockTransferResponse>();

        CreateMap<List<StockTransfer>, StockTransferResponse>();

        CreateMap<CreateStockTransferRequest, StockTransfer>();

        #endregion

        #region SALES

        CreateMap<Sale, SaleResponse>();

        CreateMap<SaleItem, SaleItemResponse>();

        CreateMap<Payment, PaymentResponse>();

        CreateMap<AddSaleItemRequest, SaleItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SaleId, opt => opt.Ignore())
            .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountAmount, opt => opt.Ignore())
            .ForMember(dest => dest.Sale, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        #endregion
    }
}