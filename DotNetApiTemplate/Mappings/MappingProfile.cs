using AutoMapper;
using DotNetApiTemplate.Models.Data;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response.Branch;

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

            CreateMap<Branch, BranchResponse>();
            CreateMap<CreateBranchRequest, Branch>();

            #endregion
        }
    }
}