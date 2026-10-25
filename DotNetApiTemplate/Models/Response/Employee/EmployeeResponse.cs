namespace SupermarketERP.Models.Response.Employee;

public class EmployeeResponse
{
    public List<BranchWorkerResponse> BranchWorkers { get; set; } = [];

    public List<WarehouseWorkerResponse> WarehouseWorkers { get; set; } = [];

}