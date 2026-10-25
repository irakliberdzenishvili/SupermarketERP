using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Employee;
using SupermarketERP.Models.Response.Employee;

namespace SupermarketERP.Interfaces
{
    public interface IEmployeeService
    {
        Task<PagedApiResponse<EmployeeResponse>> GetEmployees(
    string? role,
    bool? status,
    Guid resourceId,
    int pageNumber = 1,
    int pageSize = 10);

        Task<ApiResponse<EmployeeResponse>> CreateEmployee(CreateEmployeeRequest request);
    }
}
