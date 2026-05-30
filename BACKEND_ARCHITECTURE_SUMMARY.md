# Backend Architecture Summary

## System Overview
**Application Type:** WPF Desktop Application  
**Database:** SQL Server Express  
**Architecture Pattern:** 3-Layer Architecture (Presentation → Data Access → Database)

## Backend Components

### 1. Database Helper (`DatabaseHelper.cs`)
**Purpose:** Centralized database connection management

**Key Features:**
- Connection string management
- Connection testing functionality
- Error retrieval for diagnostics

**Current Implementation:**
```csharp
- GetConnection() - Creates new SQL connection
- TestConnection() - Validates database connectivity
- GetConnectionError() - Retrieves connection error details
```

---

### 2. Data Access Layer Classes

#### **UserDataAccess.cs**
**Responsibility:** User authentication and account management

**Key Operations:**
- `EmailExists()` - Check if email is already registered
- `AuthenticateUser()` - User login validation
- `CreateUser()` - New user registration
- `HashPassword()` - Password hashing (SHA256)
- `GetUserByEmail()` - Retrieve user information

**Security Features:**
- Password hashing using SHA256
- Active user validation
- SQL injection protection (parameterized queries)

---

#### **ProjectDataAccess.cs**
**Responsibility:** Real estate project management

**Key Operations:**
- `GetAllProjects()` - Retrieve all projects
- `InsertProject()` - Create new project
- `UpdateProject()` - Modify existing project
- `DeleteProject()` - Remove project (with validation)
- `HasRelatedRecords()` - Check for dependencies
- `GetRelatedRecordCounts()` - Get dependency details

**Business Logic:**
- Prevents deletion of projects with related plots/sales
- Maintains referential integrity
- Tracks creation and update timestamps

---

#### **SalesDataAccess.cs**
**Responsibility:** Sales transactions and ownership management

**Key Operations:**
- `GetAllSales()` - Retrieve all sales with related data
- `InsertSale()` - Create new sale record
- `UpdateSale()` - Modify sale information
- `DeleteSale()` - Remove sale record
- `GetSaleByPlotId()` - Find sale by plot

**Complex Features:**
- Joins with Projects, Plots, and Parties tables
- Automatic plot owner updates
- Latest sale tracking for ownership
- Comprehensive sale information retrieval

---

#### **PlotDataAccess.cs**
**Responsibility:** Plot/land parcel management

**Key Operations:**
- `GetAllPlots()` - Retrieve all plots with project information

---

#### **PartyDataAccess.cs**
**Responsibility:** Customer/vendor/party management

**Operations:** (Review file for complete list)

---

#### **TransactionDataAccess.cs**
**Responsibility:** Financial transaction processing

**Operations:** (Review file for complete list)

---

#### **LedgerDataAccess.cs**
**Responsibility:** Accounting ledger entries

**Operations:** (Review file for complete list)

---

#### **PaymentPlanDataAccess.cs**
**Responsibility:** Payment plan management

**Operations:** (Review file for complete list)

---

#### **PlotManagementDataAccess.cs**
**Responsibility:** Advanced plot management operations

**Operations:** (Review file for complete list)

---

#### **PartyManagementDataAccess.cs**
**Responsibility:** Advanced party management operations

**Operations:** (Review file for complete list)

---

## Database Schema Overview

### Core Tables:
- **Users** - User accounts and authentication
- **Projects** - Real estate projects
- **Plots** - Land parcels within projects
- **Parties** - Customers, vendors, and other parties
- **Sales** - Sales transactions
- **Transactions** - Financial transactions
- **Ledger** - Accounting ledger entries
- **PaymentPlans** - Payment plan details

### Relationships:
```
Projects (1) ──→ (Many) Plots
Plots (1) ──→ (Many) Sales
Parties (1) ──→ (Many) Sales (as Buyer/Seller)
Sales ──→ Transactions
Sales ──→ PaymentPlans
```

## Design Patterns Used

1. **Repository Pattern** (Data Access Classes)
   - Each entity has dedicated DataAccess class
   - Encapsulates database operations

2. **Singleton-like Pattern** (DatabaseHelper)
   - Centralized connection management
   - Reusable across all data access classes

3. **DTO Pattern** (Info classes)
   - Data transfer objects for data retrieval
   - Example: `ProjectInfo`, `SaleInfo`, `UserInfo`

## Security Features

✅ **SQL Injection Protection**
- All queries use parameterized statements
- No string concatenation in SQL queries

✅ **Password Security**
- SHA256 hashing for passwords
- Passwords never stored in plain text

✅ **Input Validation**
- Null checks and validation in data access methods
- Database constraints for data integrity

## Error Handling Strategy

- **Try-Catch Blocks:** All database operations wrapped
- **Custom Exception Messages:** User-friendly error messages
- **Error Propagation:** Exceptions bubble up with context
- **Connection Error Handling:** Dedicated methods for connection issues

## Code Quality Features

✅ **Consistent Naming:** Clear, descriptive method names  
✅ **Separation of Concerns:** Each class has single responsibility  
✅ **Reusability:** DatabaseHelper used across all classes  
✅ **Maintainability:** Modular structure allows easy updates  

## Technology Stack

- **.NET 8.0** - Framework
- **WPF** - Presentation framework
- **Microsoft.Data.SqlClient** - Database connectivity
- **SQL Server Express** - Database server
- **C#** - Programming language

## Key Strengths for Presentation

1. **Clean Architecture** - Well-organized layers
2. **Security** - SQL injection protection, password hashing
3. **Maintainability** - Modular, reusable code
4. **Scalability** - Easy to extend with new features
5. **Error Handling** - Comprehensive exception management
6. **Data Integrity** - Referential integrity checks

## Areas for Enhancement (Optional)

1. **Configuration Management** - Move connection string to config file
2. **Service Layer** - Add business logic layer
3. **Logging** - Implement structured logging
4. **Unit Testing** - Add test coverage
5. **Async/Await** - Consider async database operations
6. **Dependency Injection** - For better testability

---

**Last Updated:** Generated for presentation preparation



