SupermarketERP
SupermarketERP is a backend-focused supermarket management system built with ASP.NET Core Web API. It models operations across branches and warehouses, including employee assignments, product catalog management, inventory tracking, and controlled stock movements.
Project status: Active development. Core management and inventory functionality is implemented; the stock-transfer workflow is being developed.

Tech Stack
- C# / ASP.NET Core Web API — REST endpoints and business logic
- Entity Framework Core / SQL Server — persistence, relationships, migrations, and queries
- ASP.NET Core Identity — users and role management
- AutoMapper — entity/DTO mapping
- ASP.NET Core rate limiting — endpoint-specific request limits
- Custom exception middleware and API response wrappers — consistent error and response handling
Implemented Features
Authentication and authorization
- Identity-based user management and role-based authorization
- Resource-level access checks for assigned branches and warehouses
- Roles including Admin, BranchManager, WarehouseManager, and operational staff
Branches and warehouses
- Create, update, retrieve, and manage branches and warehouses
- Assign managers and employees to their workplaces
- Apply business rules when activating locations
- Soft-delete and active-status handling where applicable
Employees
- Create and manage employee records
- Assign roles and workplace associations
- Restrict management operations based on the current user's role and assigned location
Products
- Manage product details, SKU, optional barcode, category, and pricing
- Validate uniqueness and support paginated product queries
- Handle active/inactive products and soft deletion
Inventory
- Track product quantities separately by warehouse or branch
- Configure minimum stock thresholds and query low-stock items
- Filter inventory by product and location
- Record signed inventory adjustments with reasons, timestamps, and the acting user
- Retrieve adjustment history for auditing
- Enforce role-specific inventory access
Stock Transfers — In Progress
The stock-transfer module manages warehouse-to-branch requests through a controlled lifecycle:
Pending → Approved → Dispatched → Received
   ├──→ Rejected
   └──→ Cancelled
- Request: A branch requests a product and quantity from its associated warehouse.
- Approval: An authorized warehouse manager approves a pending request.
- Dispatch (planned): Warehouse inventory is deducted after validating available stock.
- Receipt (planned): Branch inventory is credited when the shipment is received.
- Rejection/cancellation (planned): Invalid or unwanted pending requests are closed without moving stock.
The approval workflow uses an EF Core conditional update (ExecuteUpdateAsync) so that only a transfer still in Pending status can be approved. Dispatch and receipt require transactional inventory changes and concurrency safeguards.
API Design
The API follows a controller/service structure with DTO-based request and response models. Examples of inventory routes include:
Method	Route	Purpose
GET	/api/inventory	List inventory (Admin)
GET	/api/inventory/warehouses/{warehouseId}	Warehouse inventory
GET	/api/inventory/branches/{branchId}	Branch inventory
GET	/api/inventory/low-stock	Low-stock overview
PATCH	/api/inventory/warehouses/{warehouseId}/{inventoryId}/adjust	Adjust warehouse stock
PATCH	/api/inventory/branches/{branchId}/{inventoryId}/adjust	Adjust branch stock


Stock transfer routes are being added under /api/stock-transfers.
Business Rules
- Managers can operate only on their assigned locations; Admin has broader access.
- Warehouse and branch inventories are distinct records.
- Manual inventory adjustments record who made the change and why.
- Transfer approval does not change inventory quantities; stock moves at dispatch and receipt.
- Transfer status transitions are validated to prevent invalid repeated operations.
- Pagination, validation, and endpoint-specific rate limiting are used throughout the API.
Running Locally
Prerequisites: .NET SDK compatible with the project's target framework and SQL Server.
1. Clone the repository:
   git clone https://github.com/irakliberdzenishvili/SupermarketERP.git
   cd SupermarketERP
2. Configure the SQL Server connection string and any authentication-related settings using local configuration or .NET user secrets. Do not commit credentials.
3. Restore dependencies and apply migrations from the appropriate project directory:
   dotnet restore
   dotnet ef database update
4. Start the API from its project directory:
   dotnet run
Depending on the solution structure, specify --project and --startup-project for EF Core commands. Consult the project configuration for required settings.

Development Roadmap
- [x] Branch and warehouse management
- [x] Employee and role management
- [x] Product catalog
- [x] Inventory and low-stock queries
- [x] Inventory adjustment history
- [ ] Complete stock-transfer approval, rejection, dispatch, receipt, and cancellation workflows
- [ ] Add transactional stock movement and concurrency safeguards
- [ ] Expand automated testing
Repository
GitHub — SupermarketERP
Author
Irakli Berdzenishvili — .NET Backend Developer
