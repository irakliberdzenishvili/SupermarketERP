using AutoMapper;
using DotNetApiTemplate.Models.Data;
using SupermarketERP.Models.Data.Branch;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response.BranchManagerResponse;
using SupermarketERP.Models.Response.EmployeeResponse;

namespace DotNetApiTemplate.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Add mappings here
            // Example:
            // CreateMap<User, UserResponse>();

            #region BRANCHES

            CreateMap<Branch, BranchResponse>()
    .ForMember(dest => dest.IsManager,
        opt => opt.Ignore());
            CreateMap<CreateBranchRequest, Branch>();

            CreateMap<UpdateBranchRequest, Branch>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.Code, opt => opt.Ignore())
    .ForMember(dest => dest.BranchManagerId, opt => opt.Ignore())
    .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            #endregion


            #region EMPLOYEES

            CreateMap<Employee, EmployeeResponse>();

            CreateMap<User, BranchManagerResponse>();

            CreateMap<Branch, BranchResponse>()
                .ForMember(dest => dest.IsManager,
                    opt => opt.Ignore());
            #endregion
        }
    }
}