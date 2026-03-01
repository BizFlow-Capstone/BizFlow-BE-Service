# Product Flow Documentation

> **Mục đích**: Document chi tiết các flow liên quan đến quản lý sản phẩm (Product) trong BizFlow Platform.

---

## Mục lục

1. [Tổng quan Product](#1-tổng-quan-product)
2. [Entity Design](#2-entity-design)
3. [Create Product Flow](#3-create-product-flow)
4. [View & Search Product Flow](#4-view--search-product-flow)
5. [Update Product Flow](#5-update-product-flow)
6. [Delete Product Flow](#6-delete-product-flow)
7. [Status Management](#7-status-management)
8. [SaleItem & Pricing Rules](#8-saleitem--pricing-rules)
9. [Stock Rules & Integration](#9-stock-rules--integration)
10. [API Endpoints Summary](#10-api-endpoints-summary)
11. [Error Codes](#11-error-codes)

---

## 1. Tổng quan Product

### Context - Nghiệp vụ quản lý sản phẩm HKD

Đối với hộ kinh doanh Việt Nam:

- Sản phẩm đa dạng: hàng hóa vật lý (tạp hóa, VLXD), dịch vụ (cắt tóc, sửa xe), F&B (quán ăn, cà phê)
- Một sản phẩm có thể bán theo **nhiều đơn vị** khác nhau (bao, thùng, kg, cái...)
- Giá bán có thể thay đổi theo thời điểm (giá mùa, giá khuyến mãi)
- Tồn kho chỉ áp dụng cho **hàng hóa vật lý**, dịch vụ/F&B không cần quản lý kho

### Core Concepts

```markdown
Product (Sản phẩm)
├── Thông tin chung: Tên, SKU, Ảnh, Nhà sản xuất
├── Tồn kho: Stock (theo base unit), CostPrice (giá vốn)
├── Flag: TrackInventory (có quản lý kho không?)
│
├── SaleItem 1 (Base Unit - tự động tạo)
│   ├── Unit: "Bao" (= Product.Unit)
│   ├── Quantity: 1
│   └── PricePolicy: 95,000đ (default)
│
├── SaleItem 2 (Price Tier - user tạo thêm)
│   ├── Unit: "Thùng"
│   ├── Quantity: 12 (= 12 bao)
│   └── PricePolicy: 1,080,000đ (= 90,000đ/bao)
│
└── SaleItem 3 (Price Tier - user tạo thêm)
    ├── Unit: "Tấn"
    ├── Quantity: 20 (= 20 bao)
    └── PricePolicy: 1,800,000đ (= 90,000đ/bao)
```

**Quan hệ giữa các entity:**

```markdown
Product 1 ──── N SaleItem 1 ──── N ProductPricePolicy
   │                │                    │
   │                │                    ├── Price (giá bán)
   │                │                    ├── IsDefault (giá mặc định?)
   │                │                    ├── StartAt (bắt đầu áp dụng)
   │                │                    └── EndAt (kết thúc)
   │                │
   │                ├── Unit (đơn vị bán: thùng, bao...)
   │                └── Quantity (quy đổi: 1 thùng = 12 bao)
   │
   ├── Unit (đơn vị tính nhỏ nhất / base unit)
   ├── Stock (tồn kho theo base unit)
   ├── SellingPrice (giá bán theo base unit)
   └── TrackInventory (có quản lý tồn kho?)
```

### Phân biệt Product vs SaleItem

| Khía cạnh | Product | SaleItem |
|-----------|---------|----------|
| **Mục đích** | Quản lý hàng hóa trong kho (inventory) | Quản lý đơn vị bán hàng (sales) |
| **Scope** | Thông tin chung của sản phẩm | Đơn vị + giá bán cụ thể |
| **Unit** | Base unit (đơn vị nhỏ nhất) | Đơn vị bán (có thể = base hoặc lớn hơn) |
| **Stock** | Có (nếu TrackInventory) | Không |
| **Giá** | CostPrice (giá vốn) | PricePolicy (giá bán) |
| **Ví dụ** | Xi măng Hà Tiên - Stock 150 bao | Bao (1) - 95k, Thùng (12 bao) - 1.08M |

> **Lưu ý dịch vụ/F&B**: Đối với ngành dịch vụ và F&B, sản phẩm không cần quản lý tồn kho → `TrackInventory = false`. SaleItem vẫn dùng để quản lý giá bán.

---

## 2. Entity Design

### Tables

```sql
-- =============================================
-- PRODUCTS TABLE (Sản phẩm)
-- =============================================
CREATE TABLE Products (
    ProductId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL COMMENT 'Thuộc location nào',
    BusinessTypeId CHAR(36) NOT NULL COMMENT 'Loại hình kinh doanh',
    
    -- Thông tin sản phẩm
    ProductName VARCHAR(255) NOT NULL,
    Sku VARCHAR(100) DEFAULT NULL COMMENT 'Mã SKU (optional)',
    Unit VARCHAR(50) NOT NULL COMMENT 'Đơn vị tính nhỏ nhất (base unit)',
    
    -- Giá & Tồn kho
    SellingPrice DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Giá bán theo base unit',
    Stock INT NOT NULL DEFAULT 0 COMMENT 'Tồn kho theo base unit',
    
    -- Ảnh sản phẩm (Cloudinary)
    ImageUrl VARCHAR(500) DEFAULT NULL,
    ImagePublicId VARCHAR(255) DEFAULT NULL COMMENT 'Cloudinary public ID',
    
    -- Metadata
    Manufacturer VARCHAR(255) DEFAULT NULL COMMENT 'Nhà sản xuất',
    TrackInventory BOOLEAN NOT NULL DEFAULT TRUE COMMENT 'Có quản lý tồn kho?',
    Status ENUM('active', 'inactive') NOT NULL DEFAULT 'active',
    
    -- Soft delete
    DeletedAt DATETIME DEFAULT NULL COMMENT 'Soft delete timestamp',
    
    -- FKs & Indexes
    CONSTRAINT fk_product_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocations(BusinessLocationId),
    CONSTRAINT fk_product_business_type FOREIGN KEY (BusinessTypeId) 
        REFERENCES BusinessTypes(BusinessTypeId),
    INDEX idx_product_location (BusinessLocationId),
    INDEX idx_product_sku (Sku),
    INDEX idx_product_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- SALE_ITEMS TABLE (Đơn vị bán hàng)
-- =============================================
CREATE TABLE SaleItems (
    SaleItemId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProductId BIGINT NOT NULL,
    
    Unit VARCHAR(50) NOT NULL COMMENT 'Đơn vị bán (bao, thùng, kg...)',
    Quantity INT NOT NULL DEFAULT 1 COMMENT 'Quy đổi: 1 Unit = N base units',
    
    -- Soft delete
    DeletedAt DATETIME DEFAULT NULL,
    
    CONSTRAINT fk_saleitem_product FOREIGN KEY (ProductId) 
        REFERENCES Products(ProductId),
    INDEX idx_saleitem_product (ProductId),
    INDEX idx_saleitem_deletedat (DeletedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =============================================
-- PRODUCT_PRICE_POLICIES TABLE (Chính sách giá)
-- =============================================
CREATE TABLE ProductPricePolicies (
    ProductPricePolicyId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SaleItemId BIGINT NOT NULL,
    
    Price DECIMAL(15,2) NOT NULL COMMENT 'Giá bán',
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Giá mặc định?',
    StartAt DATETIME DEFAULT NULL COMMENT 'Bắt đầu áp dụng',
    EndAt DATETIME DEFAULT NULL COMMENT 'Kết thúc áp dụng (NULL = vô thời hạn)',
    
    CONSTRAINT fk_pricepolicy_saleitem FOREIGN KEY (SaleItemId) 
        REFERENCES SaleItems(SaleItemId),
    INDEX idx_pricepolicy_saleitem (SaleItemId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### Entity Classes

```csharp
public partial class Product
{
    public long ProductId { get; set; }
    public int BusinessLocationId { get; set; }
    public Guid BusinessTypeId { get; set; }
    
    public string ProductName { get; set; } = null!;
    public string? Sku { get; set; }
    public string Unit { get; set; } = null!;         // Base unit
    
    public decimal CostPrice { get; set; }
    public int Stock { get; set; }
    
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
    public string? Manufacturer { get; set; }
    public bool? TrackInventory { get; set; }
    public string Status { get; set; } = null!;        // "active" | "inactive"
    public DateTime? DeletedAt { get; set; }
    
    // Navigation
    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual BusinessType BusinessType { get; set; } = null!;
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<ProductImport> ProductsImports { get; set; } = new List<ProductImport>();
}

public partial class SaleItem
{
    public long SaleItemId { get; set; }
    public long ProductId { get; set; }
    
    public string Unit { get; set; } = null!;          // Đơn vị bán
    public int Quantity { get; set; }                   // Quy đổi ra base unit
    public DateTime? DeletedAt { get; set; }
    
    // Navigation
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<ProductPricePolicy> ProductPricePolicies { get; set; } = new List<ProductPricePolicy>();
}

public partial class ProductPricePolicy
{
    public long ProductPricePolicyId { get; set; }
    public long SaleItemId { get; set; }
    
    public decimal Price { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    
    // Navigation
    public virtual SaleItem SaleItem { get; set; } = null!;
}
```

### Relationship Diagram

```
┌─────────────────────────────┐
│         Product             │
│─────────────────────────────│
│ ProductId (PK)              │
│ BusinessLocationId (FK)     │
│ BusinessTypeId (FK)         │
│ ProductName                 │
│ Sku                         │
│ Unit ◄───── Base unit       │
│ CostPrice                   │
│ Stock                       │
│ ImageUrl / ImagePublicId    │
│ TrackInventory              │
│ Status                      │
│ DeletedAt                   │
└──────────┬──────────────────┘
           │ 1:N
           ▼
┌─────────────────────────────┐
│        SaleItem             │
│─────────────────────────────│
│ SaleItemId (PK)             │
│ ProductId (FK)              │
│ Unit                        │
│ Quantity ◄── Quy đổi        │
│ DeletedAt                   │
└──────────┬──────────────────┘
           │ 1:N
           ▼
┌─────────────────────────────┐
│    ProductPricePolicy       │
│─────────────────────────────│
│ ProductPricePolicyId (PK)   │
│ SaleItemId (FK)             │
│ Price                       │
│ IsDefault                   │
│ StartAt / EndAt             │
└─────────────────────────────┘
```

---

## 3. Create Product Flow

### Permission

| Action | Owner | Employee | Admin | Consultant |
|--------|:-----:|:--------:|:-----:|:----------:|
| Tạo sản phẩm | ✅ | ❌ | ❌ | ❌ |

### Input

| Field | Bắt buộc? | Validation | Mặc định |
|-------|:---------:|------------|----------|
| LocationId | ✅ | Phải là location mà user owns | — |
| BusinessTypeId | ✅ | Phải tồn tại | — |
| ProductName | ✅ | Max 255 chars | — |
| Unit | ✅ | Max 50 chars (base unit) | — |
| Sku | ❌ | Max 100 chars | `null` |
| SellingPrice | ❌ | >= 0 | `0` |
| Stock | ❌ | integer | `0` |
| TrackInventory | ❌ | boolean | `true` |
| Manufacturer | ❌ | Max 255 chars | `null` |
| Image | ❌ | File (multipart) | `null` |
| PriceTiers | ❌ | JSON array | `[]` |

#### PriceTier Schema

```json
[
  {
    "Unit": "Thùng",       // Đơn vị bán (khác base unit)
    "Quantity": 12,         // 1 Thùng = 12 [base unit]
    "Price": 1080000        // Giá bán cho 1 Thùng
  }
]
```

### Flow Diagram

```
Owner tạo sản phẩm mới
        │
        ▼
┌─────────────────────────────┐
│ 1. Validate Ownership       │
│    IsOwner(userId, locId)?  │
│    → 403 nếu không phải     │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 2. Validate Input           │
│    - Required fields        │
│    - PriceTier unit ≠ base  │
│      unit (no duplicate)    │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 3. Create Product entity    │
│    Status = 'active'        │
│    DeletedAt = null         │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 4. Auto-create Default      │
│    SaleItem (base unit)     │
│    ├── Unit = Product.Unit  │
│    ├── Quantity = 1         │
│    └── PricePolicy:         │
│        Price = CostPrice    │
│        IsDefault = true     │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 5. Create PriceTier         │
│    SaleItems (nếu có)       │
│    Mỗi tier → SaleItem +    │
│    PricePolicy (IsDefault)  │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 6. Upload Image (nếu có)    │
│    → Cloudinary             │
│    → Lưu URL + PublicId     │
│    → Rollback nếu DB fail   │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 7. SaveChanges              │
│    → Nếu fail: xóa image    │
│      đã upload              │
└───────────┬─────────────────┘
            │
            ▼
        ✅ Return ProductListItemDto
```

### Business Rules

**RULE-PROD-01: Default SaleItem tự động tạo**

Khi tạo product, hệ thống **luôn** tạo 1 SaleItem mặc định:

- `Unit` = Product.Unit (base unit)
- `Quantity` = 1
- `PricePolicy.Price` = Product.SellingPrice
- `PricePolicy.IsDefault` = true

> ⚠️ **Điểm thảo luận**: Hiện tại default SaleItem lấy giá = `SellingPrice` (giá bán). Nhưng giá vốn thường **khác** giá bán. Cần xem xét: có nên yêu cầu user nhập `SellingPrice` riêng cho base unit không? Hay để user tự cập nhật sau?

**RULE-PROD-02: PriceTier Unit không được trùng Base Unit**

```csharp
// Validate: không cho PriceTier có Unit giống Product.Unit
var duplicateUnitTier = request.PriceTiers.FirstOrDefault(t => 
    string.Equals(t.Unit?.Trim(), request.Unit?.Trim(), StringComparison.OrdinalIgnoreCase));
    
if (duplicateUnitTier != null)
    throw new BadRequestException("Duplicate unit in price tiers");
```

> Vì base unit đã có SaleItem mặc định → PriceTier chỉ dành cho đơn vị khác.

**RULE-PROD-03: Image Upload Rollback**

```
Upload image → Save DB
                 │
                 ├── Success → DONE ✅
                 └── Fail → Delete uploaded image from Cloudinary → throw error
```

---

## 4. View & Search Product Flow

### Permission

| Action | Owner | Employee | Admin | Consultant |
|--------|:-----:|:--------:|:-----:|:----------:|
| Xem danh sách sản phẩm | ✅ | ✅ | ❌ | ❌ |
| Xem chi tiết sản phẩm | ✅ | ✅ | ❌ | ❌ |
| Xem sale items | ✅ | ✅ | ❌ | ❌ |

### 4.1 Search & Filter Products

**Endpoint**: `GET /api/my-business/products`

**Query Parameters:**

| Param | Type | Mô tả |
|-------|------|--------|
| LocationId | int | ✅ **Required** - Location ID |
| Name | string | Search theo tên (contains, case-insensitive) |
| Sku | string | Search theo SKU |
| MinCostPrice | decimal | Filter giá vốn tối thiểu |
| MaxCostPrice | decimal | Filter giá vốn tối đa |
| MinStock | int | Filter tồn kho tối thiểu |
| MaxStock | int | Filter tồn kho tối đa |
| Status | string | Filter theo status (`active` / `inactive`) |
| TrackInventory | bool | Filter theo flag quản lý kho |
| PageNumber | int | Trang (default: 1) |
| PageSize | int | Kích thước trang (default: 10, max: 50) |

**Response**: `PaginatedResponse<ProductListItemDto>`

```json
{
  "items": [
    {
      "productId": 1,
      "name": "Xi măng Hà Tiên",
      "sku": "XM-HT-50",
      "price": 95000,          // Default SaleItem price
      "trackInventory": true,
      "stock": 150,
      "status": "active"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

### 4.2 Product Detail

**Endpoint**: `GET /api/my-business/product/{productId}`

**Response**: `ProductDetailDto`

```json
{
  "productId": 1,
  "name": "Xi măng Hà Tiên",
  "sku": "XM-HT-50",
  "imageUrl": "https://res.cloudinary.com/.../product.jpg",
  "unit": "Bao",
  "costPrice": 80000,
  "stock": 150,
  "manufacturer": "Hà Tiên",
  "status": "active",
  "trackInventory": true,
  "businessLocationId": 1,
  "businessLocationName": "Cửa hàng VLXD Minh Anh",
  "saleItems": [
    {
      "saleItemId": 1,
      "unit": "Bao",
      "quantity": 1,
      "price": 95000
    },
    {
      "saleItemId": 2,
      "unit": "Thùng",
      "quantity": 12,
      "price": 1080000
    }
  ]
}
```

### 4.3 Product Sale Items

**Endpoint**: `GET /api/my-business/product/{productId}/sale-items`

**Response**: `ProductSaleItemsResponseDto`

```json
{
  "productId": 1,
  "saleItems": [
    { "saleItemId": 1, "unit": "Bao", "quantity": 1, "price": 95000 },
    { "saleItemId": 2, "unit": "Thùng", "quantity": 12, "price": 1080000 }
  ]
}
```

### Flow Diagram (Search)

```
User mở danh sách sản phẩm
        │
        ▼
┌─────────────────────────────┐
│ 1. Validate Access          │
│    HasAccess(userId, locId)?│
│    → Owner hoặc Employee    │
│    → 403 nếu không          │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 2. Apply Filters            │
│    - Name LIKE '%search%'   │
│    - Status = ?             │
│    - DeletedAt IS NULL      │
│    - Stock BETWEEN ? AND ?  │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 3. Paginate & Return        │
│    - Map Product → DTO      │
│    - Price = default        │
│      SaleItem's price       │
└───────────┬─────────────────┘
            │
            ▼
        ✅ PaginatedResponse
```

> **Note**: Soft deleted products (`DeletedAt IS NOT NULL`) **KHÔNG** xuất hiện trong search results.

---

## 5. Update Product Flow

### Permission

| Action | Owner | Employee |
|--------|:-----:|:--------:|
| Cập nhật thông tin sản phẩm | ✅ | ❌ |

### Input

Giống `CreateProductRequest` + thêm `RemoveImage` flag:

| Field | Mô tả |
|-------|--------|
| RemoveImage | `true` = xóa ảnh hiện tại khỏi Cloudinary |
| Image (file) | Upload ảnh mới (tự xóa ảnh cũ) |

### Flow Diagram

```
Owner cập nhật sản phẩm
        │
        ▼
┌─────────────────────────────┐
│ 1. Validate Product exists  │
│    GetByIdWithSaleItems()   │
│    → 404 nếu không tìm thấy│
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 2. Validate Ownership       │
│    IsOwner(userId, locId)?  │
│    → 403 nếu không phải     │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 3. Validate Location        │
│    không đổi Location       │
│    → 400 nếu locationId     │
│      khác product hiện tại  │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 4. Validate PriceTier       │
│    Unit ≠ base unit         │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 5. Update Product fields    │
│    Name, SKU, CostPrice,    │
│    Stock, Unit, etc.        │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 6. Handle Image             │
│    ├── RemoveImage=true     │
│    │   → Delete từ Cloud    │
│    └── New Image            │
│        → Delete cũ + Upload │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 7. Sync SaleItems           │◄─── RULE-PROD-04
│    (xem chi tiết bên dưới)  │
└───────────┬─────────────────┘
            │
            ▼
        ✅ Return ProductListItemDto
```

### RULE-PROD-04: SaleItem Sync Strategy (Reconciliation)

Khi update product, hệ thống **reconcile** SaleItems thay vì delete-all + re-create:

```
Expected SaleItems (từ request):
├── (Unit=Product.Unit, Qty=1, Price=CostPrice)    ← Default
├── PriceTier 1
└── PriceTier 2

Existing SaleItems (trong DB):
├── SaleItem A
├── SaleItem B
└── SaleItem C

Reconciliation:
1. Match expected vs existing by (Unit + Quantity)
2. Matched → Update PricePolicy price
3. Not matched → Create new SaleItem + PricePolicy
4. Remaining existing (unmatched) → Soft delete (DeletedAt = now)
```

**Tại sao reconcile thay vì delete-all?**
- SaleItem có thể đã được reference trong OrderDetail
- Hard delete sẽ break FK constraints
- Soft delete giữ lại lịch sử

### RULE-PROD-05: Không cho đổi Location

```csharp
if (product.BusinessLocationId != request.LocationId)
    throw new BadRequestException("Cannot change product location");
```

> Sản phẩm gắn chặt với location. Muốn chuyển location → tạo product mới ở location đích.

---

## 6. Delete Product Flow

### Permission

| Action | Owner | Employee |
|--------|:-----:|:--------:|
| Xóa sản phẩm | ✅ | ❌ |

### Flow Diagram

```
Owner xóa sản phẩm
        │
        ▼
┌─────────────────────────────┐
│ 1. Validate Product exists  │
│    → 404 nếu không tìm thấy│
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 2. Validate Ownership       │
│    → 403 nếu không phải     │
└───────────┬─────────────────┘
            │
            ▼
┌─────────────────────────────┐
│ 3. Check History            │◄─── RULE-PROD-06
│    HasHistory(productId)?   │
│    (OrderDetail, Import)    │
└───────────┬─────────────────┘
            │
       ┌────┴────┐
       │         │
    Có lịch sử  Không có
       │         │
       ▼         ▼
┌──────────┐ ┌──────────┐
│ SOFT     │ │ HARD     │
│ DELETE   │ │ DELETE   │
│ DeletedAt│ │ Remove   │
│ = now    │ │ from DB  │
└──────────┘ └──────────┘
       │         │
       └────┬────┘
            │
            ▼
        ✅ Success
```

### RULE-PROD-06: Smart Delete (Soft vs Hard)

| Điều kiện | Action | Lý do |
|-----------|--------|-------|
| Product có trong OrderDetail hoặc ProductImport | **Soft delete** (DeletedAt = now) | Giữ lại lịch sử giao dịch |
| Product chưa từng được dùng | **Hard delete** (remove from DB) | Không có reference, xóa sạch |

> **Note**: Soft deleted products không xuất hiện trong search nhưng vẫn hiển thị trong lịch sử order/import.

---

## 7. Status Management

### Permission

| Action | Owner | Employee |
|--------|:-----:|:--------:|
| Đổi status | ✅ | ❌ |

### Status Values

| Status | Mô tả | Hiển thị trong search? | Có thể tạo order? |
|--------|-------|:---------------------:|:-----------------:|
| `active` | Đang bán | ✅ | ✅ |
| `inactive` | Tạm ngưng bán | ✅ (filter) | ❌ |

### Flow

```
Owner đổi status sản phẩm
        │
        ▼
    Validate: status ∈ {'active', 'inactive'}
        │
        ▼
    Update product.Status = newStatus
        │
        ▼
    ✅ Success
```

> ✅ **Quyết định**: Bỏ trạng thái `discontinued` ở flow nghiệp vụ hiện tại. Product status chỉ gồm `active` và `inactive`.

---

## 8. SaleItem & Pricing Rules

### 8.1 SaleItem Concept (Đơn vị bán hàng)

SaleItem đại diện cho **cách bán** một sản phẩm. Ví dụ thực tế:

```
Sản phẩm: Nước ngọt Pepsi
Base Unit: Lon
Stock: 500 lon

SaleItems:
├── Lon (1 lon)           → Giá: 12,000đ
├── Lốc (6 lon)           → Giá: 65,000đ   (tiết kiệm 7k)
└── Thùng (24 lon)        → Giá: 240,000đ  (tiết kiệm 48k)
```

```
Sản phẩm: Dịch vụ cắt tóc nam
Base Unit: Lần
TrackInventory: false
Stock: N/A

SaleItems:
├── Cắt gội sấy (1 lần)  → Giá: 80,000đ
└── Combo cắt + uốn      → Giá: 250,000đ
```

### 8.2 Conversion Logic (Quy đổi khi bán)

Khi tạo Order, stock trừ theo **base unit**:

```
Bán 2 thùng Pepsi (1 thùng = 24 lon):
→ Trừ stock: 2 × 24 = 48 lon

Bán 1 lốc + 3 lon Pepsi:
→ Trừ stock: (1 × 6) + (3 × 1) = 9 lon
```

**Công thức**:
```
StockDeduction = SaleItem.Quantity × OrderDetail.Quantity
```

### 8.3 PricePolicy (Chính sách giá)

Mỗi SaleItem có thể có **nhiều PricePolicy** (giá theo thời gian):

| Field | Mô tả |
|-------|--------|
| `Price` | Giá bán |
| `IsDefault` | `true` = giá mặc định (luôn áp dụng) |
| `StartAt` | Thời điểm bắt đầu áp dụng |
| `EndAt` | Thời điểm kết thúc (`null` = vô thời hạn) |

**Quy tắc lấy giá hiện tại:**
```
1. Tìm PricePolicy có StartAt <= NOW <= EndAt (giá theo thời gian)
2. Nếu không có → Lấy PricePolicy có IsDefault = true
3. Nếu không có → Giá = 0
```

> ✅ **Quyết định**: Phase hiện tại chỉ dùng `IsDefault = true`. Logic `StartAt/EndAt` sẽ implement ở phase sau.

### 8.4 Ví dụ đầy đủ

```
Product: Xi măng Hà Tiên PCB40
    Unit: Bao (base unit)
    CostPrice: 80,000đ
    Stock: 500 bao
    TrackInventory: true

    SaleItem 1 (default - auto created):
        Unit: Bao
        Quantity: 1
        PricePolicy (default): 95,000đ

    SaleItem 2 (price tier):
        Unit: Thùng (10 bao)
        Quantity: 10
        PricePolicy (default): 900,000đ  (= 90,000đ/bao, giảm 5k/bao)

    SaleItem 3 (price tier):
        Unit: Pallet (50 bao)
        Quantity: 50
        PricePolicy (default): 4,250,000đ (= 85,000đ/bao, giảm 10k/bao)
```

---

## 9. Stock Rules & Integration

### 9.1 Stock Location

Stock được lưu trực tiếp trên `Product.Stock` (đơn vị: base unit).

### 9.2 Stock Changes

| Event | Hành động | Điều kiện |
|-------|-----------|-----------|
| **Order Completed** | Stock -= (SaleItem.Qty × OrderDetail.Qty) | `TrackInventory = true` |
| **Order Cancelled** (từ completed) | Stock += (rollback) | `TrackInventory = true` |
| **Import Confirmed** | Stock += Import.Quantity | `TrackInventory = true` |
| **Import Cancelled** (từ confirmed) | Stock -= (rollback) | `TrackInventory = true` |

### 9.3 Stock Validation Rules

**RULE-STOCK-01: Cảnh báo âm kho, không block**

```csharp
// Khi tạo order
if (product.TrackInventory == true)
{
    var requiredStock = saleItem.Quantity * orderDetail.Quantity;
    if (product.Stock < requiredStock)
    {
        // WARNING - vẫn cho tạo order
        response.Warnings.Add(new Warning
        {
            Code = "LOW_STOCK",
            Message = $"Sản phẩm {product.ProductName} chỉ còn {product.Stock} {product.Unit}"
        });
    }
}
```

> Lý do không block: HKD có thể bán hàng đang trên đường giao (stock chưa cập nhật).

**RULE-STOCK-02: TrackInventory = false**

```
Khi TrackInventory = false:
├── Order → KHÔNG trừ stock
├── Import → KHÔNG cộng stock
├── Stock field = 0 (ignored)
└── Dùng cho: dịch vụ, F&B, hàng không quản lý kho
```

### 9.4 Integration Map

```
                    ┌──────────────┐
                    │   Product    │
                    │   Stock: 500 │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
        ┌──────────┐ ┌──────────┐ ┌──────────┐
        │  Order   │ │  Import  │ │  Report  │
        │  -Stock  │ │  +Stock  │ │  Read    │
        └──────────┘ └──────────┘ └──────────┘
```

### 9.5 Rủi ro Inconsistent Stock là gì?

Inconsistent xảy ra khi `Product.Stock` không phản ánh đúng tổng biến động thực tế.

Các tình huống thường gặp:

1. **Partial failure trong transaction boundary**
    - Ví dụ: tạo Order/Import thành công nhưng update stock fail (hoặc ngược lại).
2. **Race condition / lost update**
    - 2 request cùng đọc stock cũ rồi cùng ghi đè stock mới.
3. **Manual fix trực tiếp DB**
    - Chỉnh tay stock mà không có log biến động tương ứng.
4. **Rollback nghiệp vụ không đối xứng**
    - Complete có trừ stock, nhưng Cancel/Undo bị miss một nhánh xử lý.

Hệ quả:

- Báo cáo tồn kho sai.
- Cảnh báo low stock sai.
- Quyết định nhập hàng sai.

### 9.6 StockMovements log giúp gì?

`StockMovements` là nhật ký biến động kho theo từng nghiệp vụ (+/-), giúp:

1. **Audit trail đầy đủ**
    - Biết chính xác ai, khi nào, lý do gì làm stock tăng/giảm.
2. **Reconciliation / tự kiểm tra sai lệch**
    - So sánh `Product.Stock` với `SUM(StockMovements.QuantityDelta)` để phát hiện lệch.
3. **Khả năng rebuild stock**
    - Nếu stock hiện tại bị sai, có thể tính lại từ log.
4. **Dễ điều tra incident**
    - Truy vết nhanh bug do flow Order/Import/Cancel.

**Khuyến nghị kiến trúc cho BizFlow (practical):**

- Giữ `Product.Stock` để query nhanh (read path).
- Thêm `StockMovements` làm immutable log (write path/audit).
- Cập nhật cả 2 trong cùng transaction cho các action Complete/Cancel Import-Order.
- Thêm job reconcile định kỳ để cảnh báo lệch.

---

## 10. API Endpoints Summary

| Method | Endpoint | Mô tả | Permission |
|--------|----------|-------|------------|
| `GET` | `/api/my-business/products?LocationId=1&...` | Search/filter products | Owner, Employee |
| `GET` | `/api/my-business/product/{productId}` | Product detail + SaleItems | Owner, Employee |
| `GET` | `/api/my-business/product/{productId}/sale-items` | Sale items only | Owner, Employee |
| `POST` | `/api/my-business/product` | Create product (multipart) | Owner only |
| `PUT` | `/api/my-business/product/{id}` | Update product (multipart) | Owner only |
| `PUT` | `/api/my-business/product/{productId}/status` | Toggle status | Owner only |
| `DELETE` | `/api/my-business/product/{productId}` | Soft/Hard delete | Owner only |

### Request/Response Summary

| Endpoint | Request DTO | Response DTO |
|----------|-------------|-------------|
| Search | `ProductQueryParams` | `PaginatedResponse<ProductListItemDto>` |
| Detail | — | `ProductDetailDto` |
| Sale Items | — | `ProductSaleItemsResponseDto` |
| Create | `CreateProductRequest` + file | `ProductListItemDto` |
| Update | `UpdateProductRequest` + file | `ProductListItemDto` |
| Status | `UpdateProductStatusRequest` | — |
| Delete | — | — |

---

## 11. Error Codes

| Code | HTTP | Mô tả |
|------|:----:|-------|
| `FORBIDDEN` | 403 | User không có quyền (không phải Owner hoặc không access được location) |
| `NOT_FOUND` | 404 | Product không tồn tại |
| `PRODUCT_DUPLICATE_UNIT_IN_PRICE_TIERS` | 400 | PriceTier có unit trùng base unit |
| `PRODUCT_CANNOT_CHANGE_LOCATION` | 400 | Không cho đổi location khi update |
| `PRODUCT_INVALID_STATUS` | 400 | Status không hợp lệ |
| `PRODUCT_IMAGE_UPLOAD_FAILED` | 400 | Upload ảnh lên Cloudinary thất bại |

---

## 📌 Chốt quyết định hiện tại

### D1. Có nên đổi Product.CostPrice thành SellingPrice?

**Kết luận kiến trúc**: **Không nên đổi tên/đổi nghĩa `CostPrice` thành `SellingPrice`**.

Lý do:

- `CostPrice` và `SellingPrice` là 2 bản chất dữ liệu khác nhau.
- `SellingPrice` đã thuộc domain của `SaleItem + ProductPricePolicy` (đa đơn vị, đa mức giá).
- Nếu đổi `CostPrice` thành `SellingPrice`, phần báo cáo lãi/lỗ và đối soát chi phí sẽ khó đúng nghĩa.

Hướng phù hợp:

- Giữ `Product.CostPrice` = giá vốn tham chiếu hiện tại (manual).
- Giữ giá bán tại `ProductPricePolicy.Price`.
- Nếu cần tính chi phí chi tiết: query từ bảng chi phí chuyên biệt + lịch sử `ProductImport`.

### D2. Status `discontinued`

**Quyết định**: Bỏ trong scope hiện tại. Chỉ dùng `active` / `inactive`.

### D3. Time-based Pricing (`StartAt/EndAt`)

**Quyết định**: Để phase sau. Phase hiện tại chỉ dùng default price (`IsDefault = true`).

### D4. Quản lý CostPrice

**Quyết định**: `CostPrice` do user nhập tay.

Hỗ trợ UX đề xuất:

- Khi tạo/cập nhật product: gợi ý `CostPrice` từ import gần nhất hoặc trung bình gần nhất.
- Khi nhập hàng: gợi ý nhanh NCC, giá vốn gần nhất, mô tả lần nhập trước để user xác nhận một chạm.
- User vẫn là người quyết định cuối cùng (explicit confirm).

### D5. Stock consistency

**Kết luận**: Dùng mô hình **Hybrid** cho roadmap: `Product.Stock` + `StockMovements` log để cân bằng performance và audit/reconciliation.
