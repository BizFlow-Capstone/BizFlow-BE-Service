# BizFlow - C# Backend Rules

## Project Context

BizFlow - Nền tảng hỗ trợ chuyển đổi số cho hộ kinh doanh Việt Nam.

- **Framework**: ASP.NET Core 8
- **Database**: MySQL 8
- **DI Container**: Autofac
- **Architecture**: Clean Architecture (4 layers)

---

## Layer Dependencies

Domain → (no dependencies)

Application → Domain

Infrastructure→ Domain, Application

Api → All layers

---

## Architecture

### Layer Structure

BizFlow.Api/ → Controllers, Middleware, Program.cs

BizFlow.Application/ → Services, DTOs, Interfaces, Mappers, Exceptions

BizFlow.Domain/ → Entities, Enums (NO dependencies)

BizFlow.Infrastructure/→ DbContext, Repositories, External Services

---

## File Organization

### Naming Patterns

| Type | Pattern | Location | Example |
|------|---------|----------|---------|

| Controller | `{Feature}Controller.cs` | `Api/Controllers/` | `ProductController.cs` |
| Service Interface | `I{Feature}Service.cs` | `Application/Interfaces/Services/` | `IProductService.cs` |
| Service | `{Feature}Service.cs` | `Application/Services/` | `ProductService.cs` |
| Repository Interface | `I{Entity}Repository.cs` | `Application/Interfaces/Repositories/` | `IProductRepository.cs` |
| Repository | `{Entity}Repository.cs` | `Infrastructure/Repositories/` | `ProductRepository.cs` |
| Entity | `{Name}.cs` | `Domain/Entities/` | `Product.cs` |
| DTO | `{Name}Dto.cs` | `Application/DTOs/{Feature}/` | `ProductDto.cs` |
| Request | `{Action}{Entity}Request.cs` | `Application/DTOs/{Feature}/` | `CreateProductRequest.cs` |
| Query Params | `{Entity}QueryParams.cs` | `Application/DTOs/{Feature}/` | `ProductQueryParams.cs` |
| Mapper | `{Entity}Mapper.cs` | `Application/Mappers/` | `ProductMapper.cs` |
| Exception | `{Name}Exception.cs` | `Application/Common/Exceptions/` | `NotFoundException.cs` |

---

## Controller Pattern

```csharp
[Route("api/{controller}s")]
[Authorize]  // Optional in development states
public class ProductController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductController(
        IProductService productService,
        IMessageService messageService,
        ILogger<ProductController> logger)
        : base(messageService, logger)
    {
        _productService = productService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get products with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductQueryParams query)
    {
        // NOT using try-catch - GlobalExceptionMiddleware already handle this
        var userId = ClaimsPrincipalExtensions.GetUserId();
        var result = await _productService.SearchProductsAsync(userId, query);
        return OkPaginated(result, MessageKeys.ProductsRetrievedSuccessfully);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var userId = ClaimsPrincipalExtensions.GetUserId();
        var result = await _productService.CreateProductAsync(userId, request);
        return Created(result, MessageKeys.ProductCreatedSuccessfully, 
            nameof(GetById), new { id = result.ProductId });
    }
}
```

Controller Rules

- Inheriting BaseApiController
- Using [Authorize] for all endpoints (excluding authen APIs, optional in development states)
- Get userId by using ClaimsPrincipalExtensions
- Using MessageKeys constants
- DO NOT try-catch (GlobalExceptionMiddleware handles)  
- NO business logic (delegate to Service)

---

## Service Pattern

```csharp
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductListItemDto> CreateProductAsync(Guid userId, CreateProductRequest request)
    {
        // 1. Validate business access
        var isOwner = await _unitOfWork.BusinessLocations
            .IsOwnerOfLocationAsync(userId, request.LocationId);
        if (!isOwner)
        {
            throw new ForbiddenException(MessageKeys.LocationAccessDenied);
        }

        // 2. Create entity
        var product = new Product
        {
            BusinessLocationId = request.LocationId,
            ProductName = request.Name,
            // ... mapping
        };

        // 3. Persist
        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // 4. Return DTO (never return Entity)
        return ProductMapper.ToListItemDto(product);
    }
}
```

Service rules

- Inject IUnitOfWork (not individual repositories)

- Validate business ownership before manipulating data

- Throw custom exceptions: NotFoundException, ForbiddenException, BadRequestException

- Return DTOs, not Entities

- Use MessageKeys for exception messages

---

## Repository Pattern

```csharp
public class ProductRepository : IProductRepository
{
    private readonly BizFlowDbContext _dbContext;

    public ProductRepository(BizFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(ProductQueryParams query)
    {
        var baseQuery = _dbContext.Products
            .Where(p => p.BusinessLocationId == query.LocationId)
            .Where(p => p.DeletedAt == null)  // Always filter soft-deleted
            .AsQueryable();

        baseQuery = ApplyFilters(baseQuery, query);
        
        var totalCount = await baseQuery.CountAsync();
        
        var items = await baseQuery
            .OrderByDescending(p => p.ProductId)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(p => p.SaleItems)
            .ToListAsync();

        return (items, totalCount);
    }
}
```

Repository Rules:

- Inject BizFlowDbContext
- Always filter DeletedAt == null for soft-deleted entities
- Return tuples for paginated queries: (Items, TotalCount)
- Use AsNoTracking() for read-only queries

---

DTO Rules:

- Request DTOs must have Data Annotations validation
- Mappers are located in Application/Mappers/

---

## Exception Handling

Custom Exceptions (Application layer)

```csharp
throw new NotFoundException(MessageKeys.ProductNotFound);
throw new ForbiddenException(MessageKeys.LocationAccessDenied);
throw new BadRequestException(MessageKeys.ValidationError, errors);
throw new ConflictException(MessageKeys.ProductAlreadyExists, productName);
throw new UnauthorizedException(MessageKeys.InvalidToken);
```

Exception Rules:

- Throw custom exceptions from Application/Common/Exceptions/
- Pass MessageKeys constant, no hardcode string
- GlobalExceptionMiddleware will catch and format the response
- DO NOT catch Exception in Controller

---

## Authorization Rules

Ownership Validation

```csharp
var isOwner = await _unitOfWork.BusinessLocations
    .IsOwnerOfLocationAsync(userId, locationId);
if (!isOwner)
{
    throw new ForbiddenException(MessageKeys.LocationAccessDenied);
}
```

Role-based Access

```csharp
// Controller
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ManageUsers() { }

[Authorize(Roles = "User,Admin")]
public async Task<IActionResult> CreateOrder() { }
```

## Anti-patterns to AVOID

```csharp
// Business logic in Controller
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request)
{
    // DONOT do this
    if (request.Price < 0) return BadRequest("Invalid price");
    var product = new Product { ... };
    await _dbContext.Products.AddAsync(product);
}

// Return Entity from API
public async Task<Product> GetProduct(long id) { }

// Try-catch in Controller
try { ... } catch (Exception ex) { return InternalServerError(ex); }

// Hardcoded strings
throw new NotFoundException("Product not found");

// Use DbContext in Service
private readonly BizFlowDbContext _dbContext;
```
