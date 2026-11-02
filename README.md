
# 🛒 SupermarketERP

A backend Enterprise Resource Planning (ERP) system built with **ASP.NET Core Web API**, designed to manage supermarket branches, warehouses, employees, products, inventory, and stock transfers.

The project focuses on real-world business logic, role-based authorization, inventory tracking, security, and scalable backend development.

## 🚀 Tech Stack

- C# / .NET
- ASP.NET Core Web API
- Entity Framework Core
- Microsoft SQL Server
- ASP.NET Core Identity
- JWT Authentication
- AutoMapper
- LINQ
- Dependency Injection
- ASP.NET Core Rate Limiting
- Swagger / OpenAPI

## 📦 Features

### 🔐 Authentication & Authorization

- User authentication with ASP.NET Core Identity
- JWT-based authentication
- Role-based access control
- Resource-level authorization
- Custom authorization helpers
- Rate limiting for API endpoints

### 🏢 Branch Management

- Create and manage supermarket branches
- Update branch information
- Activate and deactivate branches
- Assign branch managers
- Assign and unassign employees
- Restrict managers to their assigned branches

### 🏭 Warehouse Management

- Create and manage warehouses
- Update warehouse information
- Activate and deactivate warehouses
- Assign warehouse managers
- Manage warehouse employees
- Enforce warehouse access permissions

### 👥 Employee Management

- Create employee accounts
- Assign employee roles
- Manage workplace assignments
- Update employee information
- Activate and deactivate employees
- Filter and paginate employee records

### 🛍️ Product Management

- Create and update products
- Unique SKU identification
- Optional barcode support
- Product categories
- Purchase and selling prices
- Product activation and soft deletion
- Search, filtering, and pagination

### 📊 Inventory Management

- Warehouse inventory tracking
- Branch inventory tracking
- Product quantity management
- Minimum stock thresholds
- Low-stock detection
- Inventory lookup by product and location
- Inventory adjustments
- Adjustment history and audit tracking

### 🔄 Stock Transfers (In Development)

The stock transfer module manages product movement from warehouses to supermarket branches.

Transfer lifecycle:

Pending → Approved → Dispatched → Received

Additional states:

- Rejected
- Cancelled

Planned functionality includes:

- Branch managers requesting transfers
- Warehouse managers approving or rejecting requests
- Warehouse inventory deduction during dispatch
- Branch inventory updates upon receipt
- Transfer history and status tracking
- Concurrency-safe inventory operations

## 👤 User Roles

| Role | Responsibilities |
|------|------------------|
| Admin | Full system management |
| BranchManager | Manage assigned branch and inventory |
| WarehouseManager | Manage assigned warehouse and stock transfers |
| WarehouseWorker | Warehouse operations |
| BranchWorker | Branch operations |
| Cashier | Cashier-related operations |
| Employee | General employee access |

Access is controlled through both role-based authorization and resource ownership checks.

## 🌐 API Modules

| Module | Base Route |
|--------|------------|
| Authentication | `/api/auth` |
| Branches | `/api/branches` |
| Warehouses | `/api/warehouses` |
| Employees | `/api/employees` |
| Products | `/api/products` |
| Inventory | `/api/inventory` |
| Stock Transfers | `/api/stock-transfers` |

Actual routes may vary by controller configuration.

## 📋 Inventory API Examples

```http
GET /api/inventory
GET /api/inventory/{inventoryId}
GET /api/inventory/warehouses/{warehouseId}
GET /api/inventory/branches/{branchId}
GET /api/inventory/products/{productId}
GET /api/inventory/low-stock
GET /api/inventory/warehouses/{warehouseId}/low-stock
GET /api/inventory/branches/{branchId}/low-stock

POST /api/inventory/warehouses/{warehouseId}

PATCH /api/inventory/warehouses/{warehouseId}/{inventoryId}/adjust
PATCH /api/inventory/branches/{branchId}/{inventoryId}/adjust
```

## 🧠 Business Rules

The system implements business rules to maintain data consistency and enforce permissions.

Examples:

- Branch managers can only manage their assigned branches.
- Warehouse managers can only access their assigned warehouses.
- Products are uniquely identified by SKU.
- Inventory records are associated with a warehouse or branch.
- Inventory adjustments record quantity changes, reasons, and responsible users.
- Inventory quantities must not become negative.
- Low-stock detection uses configurable minimum quantities.
- Stock transfers follow controlled status transitions.
- Soft deletion is used where appropriate.

## 🛡️ Security

- JWT authentication
- ASP.NET Core Identity
- Role-based authorization
- Resource-level access validation
- Rate limiting
- Request validation
- Centralized exception handling
- Controlled API responses

## 🏗️ Backend Design

The project separates responsibilities across controllers, services, data access, entities, and DTOs.

Key design practices include:

- Dependency Injection
- Asynchronous database operations
- Entity Framework Core relationships
- LINQ queries and projections
- DTO mapping with AutoMapper
- Centralized error handling
- Pagination and filtering
- Database constraints and unique indexes
- Business logic encapsulation in services

## ⚙️ Getting Started

### Prerequisites

- .NET SDK compatible with the project
- SQL Server
- Visual Studio, Rider, or VS Code
- Entity Framework Core CLI tools

### Installation

Clone the repository:

```bash
git clone https://github.com/irakliberdzenishvili/SupermarketERP.git
```

Navigate into the project:

```bash
cd SupermarketERP
```

Restore dependencies:

```bash
dotnet restore
```

Configure your database connection string and required authentication settings using local configuration or user secrets.

Apply database migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run
```

If the repository contains multiple projects, run these commands from the API project directory or specify the appropriate project paths.

## 🗺️ Development Roadmap

- [x] Authentication and authorization
- [x] Branch management
- [x] Warehouse management
- [x] Employee management
- [x] Product management
- [x] Inventory management
- [x] Inventory adjustments and history
- [x] Low-stock detection
- [ ] Complete stock transfer workflow
- [ ] Sales and POS integration
- [ ] Product returns
- [ ] Reporting and analytics
- [ ] Automated unit and integration testing
- [ ] Additional concurrency and transaction safeguards

## 🎯 Project Goal

SupermarketERP is a portfolio and learning project focused on building a realistic backend system with interconnected business modules.

Rather than implementing only basic CRUD operations, the goal is to develop practical experience with business workflows, authorization, data integrity, inventory consistency, and maintainable API design.

## 👨‍💻 Author

**Irakli Berdzenishvili**

.NET Backend Developer

GitHub: https://github.com/irakliberdzenishvili
