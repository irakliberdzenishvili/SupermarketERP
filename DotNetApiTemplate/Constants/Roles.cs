namespace SupermarketERP.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string BranchManager = "BranchManager";
    public const string InventoryManager = "InventoryManager";
    public const string WarehouseWorker = "WarehouseWorker";
    public const string Cashier = "Cashier";

    public static readonly string[] All =
    [
        Admin,
        BranchManager,
        InventoryManager,
        WarehouseWorker,
        Cashier
    ];
}