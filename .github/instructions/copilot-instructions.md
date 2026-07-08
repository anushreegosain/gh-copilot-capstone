# Organization Development Standards

This document defines the coding standards, patterns, and conventions for this full-stack application. All AI-assisted code generation MUST follow these rules.

---

## Project Overview

- **Backend:** .NET 8 Web API with Entity Framework Core
- **Frontend:** React 18 with TypeScript and Tailwind CSS
- **Testing:** xUnit + Moq (backend), Jest + RTL (frontend)
- **API Style:** RESTful with envelope pattern

---

## 1. API Design Standards

### 1.1 Route Conventions

- **Base path:** `/v1/[resource]` — NO `/api/` prefix
- **Resource naming:** plural, lowercase (e.g., `/v1/companies`, `/v1/employees`, `/v1/invoices`)
- **Controller attribute:** `[Route("v1/[controller]")]`
- **No trailing slashes** on any endpoint

**Correct:**
```csharp
[Route("v1/[controller]")]
[ApiController]
public class CompaniesController : ControllerBase
```

**Wrong:**
```csharp
[Route("api/v1/[controller]")]  // No /api/ prefix
[Route("v1/[controller]/")]     // No trailing slash
```

### 1.2 Response Envelope Pattern

ALL API responses MUST be wrapped in an envelope:

**Single item response:**
```csharp
public class ItemResponseDto<T>
{
    [JsonPropertyName("item")]
    public T Item { get; set; }
    
    [JsonPropertyName("metadata")]
    public MetadataDto Metadata { get; set; }
    
    [JsonPropertyName("links")]
    public LinksDto Links { get; set; }
}
```

**Collection response:**
```csharp
public class CollectionResponseDto<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; }
    
    [JsonPropertyName("metadata")]
    public MetadataDto Metadata { get; set; }
    
    [JsonPropertyName("links")]
    public LinksDto Links { get; set; }
}
```

**Metadata structure:**
```csharp
public class MetadataDto
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; }
    
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }  // For collections only
}
```

**Links structure:**
```csharp
public class LinksDto
{
    [JsonPropertyName("self")]
    public string Self { get; set; }
    
    [JsonPropertyName("next")]
    public string? Next { get; set; }
    
    [JsonPropertyName("prev")]
    public string? Prev { get; set; }
}
```

### 1.3 Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Boolean properties | `Indicator` suffix | `activeIndicator`, `paidIndicator` |
| Boolean properties | NO `is`/`has` prefix | ❌ `isActive`, ❌ `hasPaid` |
| Date properties | `Date` suffix | `createdDate`, `updatedDate` |
| Numeric IDs | `Id` suffix | `employeeId`, `companyId` |
| Abbreviations | Avoid or expand | ❌ `nr`, ❌ `dv`, ✅ `number`, ✅ `verificationDigit` |

### 1.4 Pagination

- Use `offset` and `limit` parameters (NOT `page` and `pageSize`)
- Default: `offset=0`, `limit=20`
- Maximum limit: 100

```csharp
[HttpGet]
public async Task<ActionResult<CollectionResponseDto<CompanyDto>>> GetAll(
    [FromQuery] int offset = 0,
    [FromQuery] int limit = 20,
    CancellationToken cancellationToken = default)
```

### 1.5 Error Response Format

```csharp
public class ErrorResponseDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; }  // Format: "ORG-XXX-YYY"
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
    
    [JsonPropertyName("details")]
    public IEnumerable<ErrorDetailDto>? Details { get; set; }
}

public class ErrorDetailDto
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
}
```

**Error codes:**
- `ORG-VAL-001` — Validation error
- `ORG-NTF-001` — Resource not found
- `ORG-AUT-001` — Authentication error
- `ORG-INT-001` — Internal server error

### 1.6 Query Parameter Pattern

- Numeric `0` means "no filter" (converts to null internally)
- String empty means "no filter"

```csharp
// If companyId is 0, don't filter by company
if (query.CompanyId > 0)
{
    queryable = queryable.Where(e => e.CompanyId == query.CompanyId);
}
```

---

## 2. C# / .NET Standards

### 2.1 Async/Await

- ALL async methods MUST have `CancellationToken` as LAST parameter
- ALL async methods MUST end with `Async` suffix
- ALWAYS pass `cancellationToken` to downstream calls

**Correct:**
```csharp
public async Task<CompanyDto> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    return await _repository.GetByIdAsync(id, cancellationToken);
}
```

**Wrong:**
```csharp
public async Task<CompanyDto> GetById(int id)  // Missing Async suffix and CancellationToken
```

### 2.2 DTOs

- ALL DTOs MUST use `[JsonPropertyName("camelCase")]` on every property
- ALL DTOs MUST have XML documentation on every property
- DTOs are in `DTOs/` folder, organized by feature

```csharp
/// <summary>
/// Represents an employee in the system.
/// </summary>
public class EmployeeDto
{
    /// <summary>
    /// The unique identifier of the employee.
    /// </summary>
    [JsonPropertyName("employeeId")]
    public int EmployeeId { get; set; }
    
    /// <summary>
    /// The full name of the employee.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    /// <summary>
    /// Indicates whether the employee is currently active.
    /// </summary>
    [JsonPropertyName("activeIndicator")]
    public bool ActiveIndicator { get; set; }
}
```

### 2.3 Repository Pattern

- Interface in `Repositories/I{Name}Repository.cs`
- Implementation in `Repositories/{Name}Repository.cs`
- Use EF Core with `AsNoTracking()` for read operations
- NEVER call stored procedures — implement logic in C# with LINQ

```csharp
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<Employee>> GetAllAsync(EmployeeQuery query, CancellationToken cancellationToken);
}

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;
    
    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Employee>> GetAllAsync(
        EmployeeQuery query, 
        CancellationToken cancellationToken)
    {
        var queryable = _context.Employees.AsNoTracking();
        
        if (query.CompanyId > 0)
            queryable = queryable.Where(f => f.CompanyId == query.CompanyId);
            
        return await queryable
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
    }
}
```

### 2.4 Service Pattern

- Interface in `Services/I{Name}Service.cs`
- Implementation in `Services/{Name}Service.cs`
- Services contain business logic, call repositories

```csharp
public interface IEmployeeService
{
    Task<CollectionResponseDto<EmployeeDto>> GetAllAsync(EmployeeQuery query, CancellationToken cancellationToken);
    Task<ItemResponseDto<EmployeeDto>?> GetByIdAsync(int id, CancellationToken cancellationToken);
}

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    
    public EmployeeService(IEmployeeRepository repository)
    {
        _repository = repository;
    }
    
    // Implementation...
}
```

### 2.5 Dependency Injection

- Register all services in `Extensions/ServiceCollectionExtensions.cs`
- Use `AddScoped` for repositories and services

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        return services;
    }
}
```

### 2.6 Controller Standards

- Inherit from `ControllerBase`
- Use `[ApiController]` attribute
- Use `[ProducesResponseType]` for all response types
- Return `ActionResult<T>` wrapped in envelope

```csharp
[Route("v1/[controller]")]
[ApiController]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;
    
    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }
    
    /// <summary>
    /// Retrieves all employees with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResponseDto<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionResponseDto<EmployeeDto>>> GetAll(
        [FromQuery] EmployeeQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(query, cancellationToken);
        return Ok(result);
    }
}
```

---

## 3. Testing Standards

### 3.1 Test File Organization

```
tests/Api.Tests/
├── Controllers/
│   └── {Name}ControllerTests.cs
├── Services/
│   └── {Name}ServiceTests.cs
├── Repositories/
│   └── {Name}RepositoryTests.cs
├── Utils/
│   └── TestDataBuilders.cs
└── Integration/
    └── CustomWebApplicationFactory.cs
```

### 3.2 Test Naming Convention

`{MethodName}_{Scenario}_{ExpectedResult}`

```csharp
[Fact]
public async Task GetByIdAsync_WhenExists_ReturnsEmployee()

[Fact]
public async Task GetByIdAsync_WhenNotExists_ReturnsNull()

[Fact]
public async Task GetAllAsync_WithCompanyFilter_ReturnsFilteredResults()
```

### 3.3 AAA Pattern with Comments

ALWAYS use `// Arrange`, `// Act`, `// Assert` comments:

```csharp
[Fact]
public async Task GetByIdAsync_WhenExists_ReturnsEmployee()
{
    // Arrange
    var employee = TestDataBuilders.CreateEmployee(id: 1, name: "John Doe");
    _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(employee);
    
    // Act
    var result = await _service.GetByIdAsync(1, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("John Doe", result.Name);
}
```

### 3.4 Use #region to Organize Tests

```csharp
public class EmployeeServiceTests
{
    #region GetByIdAsync Tests
    
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEmployee() { }
    
    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull() { }
    
    #endregion
    
    #region GetAllAsync Tests
    
    [Fact]
    public async Task GetAllAsync_WithNoFilter_ReturnsAll() { }
    
    #endregion
}
```

### 3.5 TestDataBuilders Pattern

```csharp
public static class TestDataBuilders
{
    public static Employee CreateEmployee(
        int id = 1,
        string name = "Test Employee",
        int companyId = 1,
        bool active = true)
    {
        return new Employee
        {
            Id = id,
            Name = name,
            CompanyId = companyId,
            ActiveIndicator = active
        };
    }
}
```

### 3.6 Repository Tests with InMemory Database

- Use EF Core InMemory provider (NEVER SQLite)
- Create unique database name per test with `Guid.NewGuid()`
- Implement `IDisposable` to clean up

```csharp
public class EmployeeRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly EmployeeRepository _repository;
    
    public EmployeeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new EmployeeRepository(_context);
    }
    
    public void Dispose() => _context.Dispose();
}
```

### 3.7 Integration Tests

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);
            
            // Add InMemory DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));
        });
    }
}
```

---

## 4. Frontend Standards (React/TypeScript)

### 4.1 Component Structure

```
src/components/ComponentName/
├── ComponentName.tsx       # Component implementation
├── index.ts               # Re-export
└── ComponentName.test.tsx # Tests (optional for starter)
```

### 4.2 TypeScript Conventions

- Use `interface` for object shapes, `type` for unions/primitives
- Export types from `lib/types.ts`
- Use strict mode

```typescript
// lib/types.ts
export interface Employee {
  employeeId: number;
  name: string;
  activeIndicator: boolean;
}

export interface ApiResponse<T> {
  item?: T;
  items?: T[];
  metadata: {
    timestamp: string;
    transactionId: string;
    totalCount?: number;
  };
  links: {
    self: string;
    next?: string;
    prev?: string;
  };
}
```

### 4.3 API Client

```typescript
// lib/api.ts
const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export async function fetchApi<T>(
  endpoint: string,
  options?: RequestInit
): Promise<ApiResponse<T>> {
  const response = await fetch(`${API_BASE}${endpoint}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    ...options,
  });
  
  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }
  
  return response.json();
}
```

### 4.4 Styling with Tailwind

- Use Tailwind utility classes exclusively
- NO custom CSS except for brand variables
- Use CSS custom properties for brand colors

```css
/* index.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

:root {
  --color-brand-header: #87adeb;
  --color-brand-content: #e0f1fa;
  --color-brand-primary: #0066cc;
  --color-brand-text: #333333;
}
```

```tsx
// Usage in components
<header className="bg-[var(--color-brand-header)] p-4">
  <h1 className="text-white font-bold">Application</h1>
</header>
```

### 4.5 No Hardcoded Strings

- All user-facing text should be extractable for i18n
- Use constants or translation files

---

## 5. Git Workflow

### 5.1 Branch Naming

`feature/<timestamp>-<descriptive-slug>`

Example: `feature/20260206-add-employee-endpoint`

### 5.2 Commit Messages

```
<type>(<scope>): <description>

Types: feat, fix, docs, style, refactor, test, chore
Scope: backend, frontend, tests, config
```

Example: `feat(backend): add employee endpoint with filtering`

### 5.3 PR Requirements

- All tests pass
- Code coverage ≥80%
- Linting passes (`dotnet format`, `npm run lint`)
- No direct pushes to main/master

---

## 6. Forbidden Actions

❌ NEVER use `/api/` prefix in routes  
❌ NEVER use `is` or `has` prefix for boolean properties  
❌ NEVER call stored procedures from EF Core  
❌ NEVER use SQLite for tests (use InMemory only)  
❌ NEVER hardcode strings in components  
❌ NEVER skip CancellationToken in async methods  
❌ NEVER expose database column names directly in DTOs  
❌ NEVER push directly to main/master  
❌ NEVER create tests without AAA comments  
❌ NEVER skip XML documentation on DTOs  

---

## 7. Type Mapping Reference (for ASP Migration)

| Database Type | C# Type | Notes |
|---------------|---------|-------|
| SMALLINT | `short` | |
| INT | `int` | |
| BIGINT | `long` | |
| NUMERIC/DECIMAL | `decimal` | For money/precision |
| FLOAT/REAL | `double` | |
| VARCHAR/NVARCHAR | `string?` | Nullable by default |
| CHAR(1) | `char` or `string` | |
| BIT | `bool` | |
| DATETIME | `DateTime?` | Nullable by default |
| DATE | `DateOnly?` | .NET 6+ |
| TIME | `TimeOnly?` | .NET 6+ |
| UNIQUEIDENTIFIER | `Guid` | |
