# Order Flow Documentation

> **Mục đích**: Document chi tiết các flow liên quan đến đơn hàng (Order) trong BizFlow Platform.

---

## Mục lục

1. [Tổng quan Order](#1-tổng-quan-order)
2. [Order Entity Design](#2-order-entity-design)
3. [Manual Order Flow](#3-manual-order-flow)
4. [AI Voice Order Flow](#4-ai-voice-order-flow)
5. [Order Lifecycle](#5-order-lifecycle)
6. [Payment Flows](#6-payment-flows)
7. [Stock Deduction Rules](#7-stock-deduction-rules)

---

## 1. Tổng quan Order

### Context

Đơn hàng là core của hệ thống BizFlow - nơi ghi nhận mọi giao dịch bán hàng của HKD.

### Order Creation Methods

| Method | Mô tả | User |
|--------|-------|------|
| **Manual** | User chọn sản phẩm, nhập số lượng, tạo đơn | Owner, Employee |
| **AI Voice** | User nói/up file record cuộc gọi giữa owner và khách hàng, AI parse thành draft order | Owner, Employee |

### Create Order Status Flow

```markdown
                    ┌─────────────┐
                    │   PENDING   │◄─────── Đơn mới tạo (manual hoặc AI confirmed)
                    └──────┬──────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
      ┌───────────┐  ┌───────────┐  ┌───────────┐
      │ COMPLETED │  │ CANCELLED │  │  (Stay)   │
      └───────────┘  └───────────┘  └───────────┘
           │              │
           │              └─────── Hủy đơn
           │
           └─────── Hoàn tất bán hàng → Trừ kho
```

### Status Definitions

| Status | Code | Mô tả | Cho phép sửa? | Trừ kho? |
|--------|------|-------|:-------------:|:--------:|
| **Pending** | `pending` | Đơn đã xác nhận, chờ hoàn tất | ✅ | ❌ |
| **Completed** | `completed` | Đã giao hàng + thanh toán xong | ❌ | ✅ |
| **Cancelled** | `cancelled` | Đã hủy | ❌ | ❌ |

---

## 2. Order Entity Design

### Tables

```sql
-- =============================================
-- ORDERS TABLE
-- =============================================
CREATE TABLE Orders (
    OrderId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderCode VARCHAR(50) NOT NULL COMMENT 'Mã đơn hàng hiển thị cho khách',
    BusinessLocationId INT NOT NULL,
    
    -- Khách hàng (optional - có thể khách lẻ)
    DebtorId UUID DEFAULT NULL COMMENT 'Nếu là khách quen có hồ sơ',
    CustomerName VARCHAR(255) DEFAULT NULL COMMENT 'Tên khách (nếu không có debtor)',
    CustomerPhone VARCHAR(20) DEFAULT NULL,
    
    -- Tổng tiền
    SubTotal DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Tổng tiền hàng',
    Discount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Giảm giá',
    TotalAmount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Thành tiền',
    
    -- Thanh toán
    PaidAmount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền đã trả',
    DebtAmount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT 'Số tiền ghi nợ',
    PaymentMethod VARCHAR(20) DEFAULT NULL COMMENT 'cash, bank, ...',
    
    -- Status & metadata  
    Status VARCHAR(20) NOT NULL DEFAULT 'pending',
    BillMetadata JSON DEFAULT NULL,
    Note TEXT DEFAULT NULL,
    
    -- AI metadata (nếu tạo từ voice)
    IsFromAI BOOLEAN NOT NULL DEFAULT FALSE,
    AIConfidence DECIMAL(3,2) DEFAULT NULL COMMENT '0.00 - 1.00',
    OriginalTranscript TEXT DEFAULT NULL COMMENT 'Câu nói gốc',
    
    -- Audit
    CreatedByUserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CompletedAt DATETIME DEFAULT NULL,
    CancelledAt DATETIME DEFAULT NULL,
    CancelledByUserId CHAR(36) DEFAULT NULL,
    CancelReason TEXT DEFAULT NULL,
    
    -- Indexes & FKs
    CONSTRAINT fk_order_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocations(BusinessLocationId),
    CONSTRAINT fk_order_debtor FOREIGN KEY (DebtorId) 
        REFERENCES Debtors(DebtorId),
    INDEX idx_order_location (BusinessLocationId),
    INDEX idx_order_status (Status),
    INDEX idx_order_created (CreatedAt),
    INDEX idx_order_debtor (DebtorId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- ORDER DETAILS TABLE
-- =============================================
CREATE TABLE OrderDetails (
    OrderDetailId BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrderId BIGINT NOT NULL,
    
    -- Sản phẩm
    SaleItemId BIGINT NOT NULL COMMENT 'Đơn vị bán của product',
    ProductId BIGINT NOT NULL COMMENT 'Denormalized for easy query',
    ProductName VARCHAR(255) NOT NULL COMMENT 'Snapshot tên tại thời điểm bán',
    Unit VARCHAR(50) NOT NULL COMMENT 'Snapshot đơn vị',
    
    -- Số lượng & giá
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(15,2) NOT NULL COMMENT 'Giá tại thời điểm bán',
    Discount DECIMAL(15,2) NOT NULL DEFAULT 0,
    Amount DECIMAL(15,2) NOT NULL COMMENT 'Quantity × UnitPrice - Discount',
    
    -- Audit
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_order_detail_order FOREIGN KEY (OrderId) 
        REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT fk_order_detail_sale_item FOREIGN KEY (SaleItemId) 
        REFERENCES SaleItems(SaleItemId),
    INDEX idx_order_detail_order (OrderId),
    INDEX idx_order_detail_product (ProductId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### Entity Classes

```csharp
public class Order
{
    
}

public enum OrderStatus
{
    Pending,
    Completed,
    Cancelled
}
```

---

## 3. Manual Order Flow

### Overview

User tạo đơn hàng thủ công qua app/web.

### Sequence Diagram

```markdown
┌─────────┐     ┌─────────┐     ┌─────────────┐     ┌──────────┐
│  User   │     │  App    │     │  Backend    │     │    DB    │
└────┬────┘     └────┬────┘     └──────┬──────┘     └────┬─────┘
     │               │                 │                 │
     │ Chọn products │                 │                 │
     │──────────────►│                 │                 │
     │               │                 │                 │
     │ Nhập quantity │                 │                 │
     │──────────────►│                 │                 │
     │               │                 │                 │
     │ (Optional)    │                 │                 │
     │ Chọn debtor   │                 │                 │
     │──────────────►│                 │                 │
     │               │                 │                 │
     │ Chọn payment  │                 │                 │
     │──────────────►│                 │                 │
     │               │                 │                 │
     │ Submit Order  │                 │                 │
     │──────────────►│ POST /orders    │                 │
     │               │────────────────►│                 │
     │               │                 │                 │
     │               │                 │ Validate        │
     │               │                 │ - Location      │
     │               │                 │ - Products      │
     │               │                 │ - Stock warning │
     │               │                 │ - Debtor (nếu có)
     │               │                 │                 │
     │               │                 │ Calculate       │
     │               │                 │ - SubTotal      │
     │               │                 │ - Discount      │
     │               │                 │ - TotalAmount   │
     │               │                 │────────────────►│
     │               │                 │   INSERT Order  │
     │               │                 │◄────────────────│
     │               │                 │                 │
     │               │                 │ (If debt)       │
     │               │                 │────────────────►│
     │               │                 │ Update Debtor   │
     │               │                 │ CurrentBalance  │
     │               │                 │◄────────────────│
     │               │                 │                 │
     │               │◄────────────────│                 │
     │ Order created │                 │                 │
     │◄──────────────│                 │                 │
```

### API Contract

```yaml
Lần 1: FE gửi request (confirmLowStock = false hoặc không có)
  → BE validate stock → Phát hiện thiếu hàng
  → Trả 200 OK + requiresConfirmation: true + warnings[]
  → KHÔNG tạo order

Lần 2: FE hiển thị cảnh báo → User xác nhận → FE gửi lại request (confirmLowStock = true)
  → BE bỏ qua stock warning → Tạo order bình thường


Authorization: Bearer {token}

// Lần 1 - Chưa confirm
POST /api/orders
{
  "businessLocationId": 1,
  "debtorId": null,
  "customerName": "Anh Ba",
  "customerPhone": "0901234567",
  "paymentMethod": "cash",
  "paidAmount": 950000,
  "discount": 0,
  "note": "Giao trước 5h chiều",
  "isFromAI": false,
  "confirmLowStock": false,        // ← chưa xác nhận
  "items": [
    {
      "saleItemId": 123,
      "productId": 10,
      "quantity": 50,               // ← stock chỉ còn 5
      "unitPrice": 95000,
      "discount": 0
    },
    {
      "saleItemId": 456,
      "productId": 20,
      "quantity": 3,
      "unitPrice": 150000,
      "discount": 0
    }
  ]
}

Response khi cần confirm (HTTP 200, chưa tạo order):
{
  "success": true,
  "messageCode": "ORDER_STOCK_WARNING",
  "message": "Một số sản phẩm không đủ tồn kho. Vui lòng xác nhận để tiếp tục.",
  "data": {
    "requiresConfirmation": true,
    "warnings": [
      {
        "code": "LOW_STOCK",
        "productId": 10,
        "productName": "Xi măng Hà Tiên",
        "unit": "bao",
        "currentStock": 5,
        "requestedQuantity": 50,
        "message": "Xi măng Hà Tiên còn 5 bao, đơn cần 50 bao"
      }
    ]
  },
  "timestamp": "2026-02-25T10:30:00Z"
}

Request lần 2 - User đã xác nhận
// Lần 2 - Đã confirm
POST /api/orders
{
  "businessLocationId": 1,
  "customerName": "Anh Ba",
  "customerPhone": "0901234567",
  "paymentMethod": "cash",
  "paidAmount": 950000,
  "discount": 0,
  "note": "Giao trước 5h chiều",
  "isFromAI": false,
  "confirmLowStock": true,          // ← đã xác nhận, bỏ qua warning
  "items": [
    { "saleItemId": 123, "productId": 10, "quantity": 50, "unitPrice": 95000, "discount": 0 },
    { "saleItemId": 456, "productId": 20, "quantity": 3,  "unitPrice": 150000, "discount": 0 }
  ]
}

Response khi tạo thành công (HTTP 201)
{
  "success": true,
  "messageCode": "ORDER_CREATED",
  "message": "Tạo đơn hàng thành công",
  "data": {
    "requiresConfirmation": false,
    "order": {
      "orderId": 1001,
      "orderCode": "ORD-20260225-001",
      "status": "pending",
      "subTotal": 5200000,
      "discount": 0,
      "totalAmount": 5200000,
      "paidAmount": 950000,
      "debtAmount": 0,
      "createdAt": "2026-02-25T10:31:00Z"
    },
    "warnings": [
      {
        "code": "LOW_STOCK",
        "productId": 10,
        "productName": "Xi măng Hà Tiên",
        "unit": "bao",
        "currentStock": -45,
        "requestedQuantity": 50,
        "message": "Xi măng Hà Tiên tồn kho âm: -45 bao"
      }
    ]
  },
  "timestamp": "2026-02-25T10:31:00Z"
}
```

### Business Rules

**RULE-ORDER-01: Validate Location Access**

```csharp
// User phải có quyền access location
var assignment = await _unitOfWork.UserLocationAssignments
    .GetByUserAndLocationAsync(userId, request.BusinessLocationId);
if (assignment == null || !assignment.IsActive)
    throw new ForbiddenException("No access to this location");
```

**RULE-ORDER-02: Validate Products Exist**

```csharp
foreach (var item in request.Items)
{
    var saleItem = await _unitOfWork.SaleItems.GetByIdAsync(item.SaleItemId);
    if (saleItem == null)
        throw new NotFoundException($"SaleItem {item.SaleItemId} not found");
    
    // SaleItem phải thuộc location này
    if (saleItem.Product.BusinessLocationId != request.BusinessLocationId)
        throw new ValidationException($"Product not in this location");
}
```

**RULE-ORDER-03: Stock Warning (không block)**

```csharp
var warnings = new List<Warning>();
foreach (var item in request.Items)
{
    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
    if (product.TrackInventory && product.Stock < item.Quantity)
    {
        warnings.Add(new Warning
        {
            Code = "LOW_STOCK",
            Message = $"{product.ProductName} còn {product.Stock}, đơn cần {item.Quantity}"
        });
    }
}
// Vẫn tạo order, chỉ trả warning
```

**RULE-ORDER-04: Mixed Payment = Partial + Auto Debt**

```csharp
if (request.PaymentMethod == "mixed")
{
    if (request.DebtorId == null)
        throw new ValidationException("Mixed payment requires a debtor");
    
    order.PaidAmount = request.PaidAmount;
    order.DebtAmount = order.TotalAmount - request.PaidAmount;
    
    // Update debtor balance
    var debtor = await _unitOfWork.Debtors.GetByIdAsync(request.DebtorId.Value);
    debtor.CurrentBalance -= order.DebtAmount;
}
```

---

## 4. AI Voice Order Flow

### Overview

User nói/nhắn tin tự nhiên → AI parse → App hiển thị draft → User confirm → Tạo order.

**Đặc điểm quan trọng**: Draft KHÔNG lưu vào DB, chỉ trả về cho app.

### Example Inputs

| Input (Voice/Text) | AI Output |
|-------------------|-----------|
| "Lấy 5 bao xi măng cho anh Ba, ghi nợ nha" | Product: Xi măng, Qty: 5, Customer: Anh Ba, Payment: debt |
| "Bán 2 thùng bia và 3 két nước ngọt, tiền mặt" | Products: [Bia x2, Nước ngọt x3], Payment: cash |
| "Order hôm nay của chị Lan: 10kg gạo, 5 lít dầu" | Products: [Gạo 10kg, Dầu 5 lít], Customer: Chị Lan |

### Sequence Diagram

```
┌─────────┐     ┌─────────┐     ┌─────────────┐     ┌─────────────┐
│  User   │     │  App    │     │  AI Service │     │   Backend   │
└────┬────┘     └────┬────┘     └──────┬──────┘     └──────┬──────┘
     │               │                 │                   │
     │ Voice/Text    │                 │                   │
     │ "5 bao xi     │                 │                   │
     │  măng cho     │                 │                   │
     │  anh Ba"      │                 │                   │
     │──────────────►│                 │                   │
     │               │                 │                   │
     │               │ STT (if voice)  │                   │
     │               │────────────────►│                   │
     │               │                 │                   │
     │               │ Parse Order     │                   │
     │               │ (RAG + LLM)     │                   │
     │               │────────────────►│                   │
     │               │                 │                   │
     │               │                 │ Match products   │
     │               │                 │ from location    │
     │               │                 │─────────────────►│
     │               │                 │ GET /products    │
     │               │                 │◄─────────────────│
     │               │                 │                   │
     │               │ Draft Order     │                   │
     │               │ (không lưu DB)  │                   │
     │               │◄────────────────│                   │
     │               │                 │                   │
     │ Hiển thị draft│                 │                   │
     │◄──────────────│                 │                   │
     │               │                 │                   │
     │ User review   │                 │                   │
     │ & edit        │                 │                   │
     │──────────────►│                 │                   │
     │               │                 │                   │
     │ Confirm Order │                 │                   │
     │──────────────►│ POST /orders    │                   │
     │               │ (isFromAI=true) │                   │
     │               │─────────────────────────────────────►
     │               │                 │                   │
     │               │◄─────────────────────────────────────
     │ Order created │                 │                   │
     │◄──────────────│                 │                   │
```

### AI Service API

```yaml
POST /api/ai/parse-order
Authorization: Bearer {token}

Request:
{
  "businessLocationId": 1,
  "input": "Lấy 5 bao xi măng và 2 thùng gạch cho anh Ba, ghi nợ",
  "inputType": "text"    // "text" | "audio_base64"
}

Response:
{
  "success": true,
  "confidence": 0.92,
  "originalTranscript": "Lấy 5 bao xi măng và 2 thùng gạch cho anh Ba, ghi nợ",
  "parsedOrder": {
    "customerName": "Anh Ba",
    "customerPhone": null,
    "suggestedDebtorId": 45,      // Nếu match được debtor
    "paymentMethod": "debt",
    "items": [
      {
        "matchedSaleItemId": 123,
        "matchedProductName": "Xi măng Hà Tiên",
        "quantity": 5,
        "unit": "bao",
        "unitPrice": 95000,
        "confidence": 0.95,
        "originalText": "5 bao xi măng"
      },
      {
        "matchedSaleItemId": null,   // Không match được
        "matchedProductName": null,
        "quantity": 2,
        "unit": "thùng",
        "unitPrice": null,
        "confidence": 0.0,
        "originalText": "2 thùng gạch",
        "error": "Product not found: gạch"
      }
    ]
  },
  "warnings": [
    {
      "code": "PRODUCT_NOT_FOUND",
      "message": "Không tìm thấy sản phẩm: gạch"
    }
  ]
}
```

### App Draft Display

Khi nhận draft từ AI, app hiển thị:

```
┌─────────────────────────────────────────────────────────┐
│  🎤 "Lấy 5 bao xi măng và 2 thùng gạch cho anh Ba..."  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Khách hàng: [Anh Ba         ▼]  ← Dropdown debtors    │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ✅ Xi măng Hà Tiên                              │   │
│  │    5 x 95,000đ = 475,000đ           [ Edit ]   │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ ⚠️ "2 thùng gạch" - Không tìm thấy             │   │
│  │    [Chọn sản phẩm ▼]                [ Xóa ]    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ───────────────────────────────────────────────────   │
│  Tổng tiền: 475,000đ                                   │
│  Thanh toán: [Ghi nợ ▼]                                │
│                                                         │
│  [ Hủy ]                           [ Xác nhận đơn ]    │
└─────────────────────────────────────────────────────────┘
```

### Business Rules for AI

**RULE-AI-01: Always require user confirmation**

```csharp
// AI chỉ parse và suggest, KHÔNG tự động tạo order
// Draft phải được user review và confirm
```

**RULE-AI-02: Flag AI-created orders**

```csharp
// Khi user confirm draft từ AI
var order = new Order
{
    // ... other fields
    IsFromAI = true,
    AIConfidence = draftResponse.Confidence,
    OriginalTranscript = draftResponse.OriginalTranscript
};
```

**RULE-AI-03: Fallback to manual**

```csharp
// Nếu AI service không available hoặc fail
if (!aiResponse.Success)
{
    return new AiParseResponse
    {
        Success = false,
        Error = "AI service unavailable",
        FallbackMessage = "Vui lòng tạo đơn thủ công"
    };
}
```

---

## 5. Order Lifecycle

### State Transitions

```csharp
public enum OrderStatus
{
    Pending,    // Mới tạo, chờ hoàn tất
    Completed,  // Đã giao hàng + thanh toán xong
    Cancelled   // Đã hủy
}
```

### Valid Transitions

| From | To | Action | Who can do |
|------|-----|--------|------------|
| `pending` | `completed` | Complete order | Owner, Employee (own order) |
| `pending` | `cancelled` | Cancel order | Owner (any), Employee (own order) |
| `completed` | - | N/A | Không thể thay đổi |
| `cancelled` | - | N/A | Không thể thay đổi |

### Complete Order Flow

```
┌────────────────┐
│ Order Pending  │
└───────┬────────┘
        │
        ▼
┌────────────────────────┐
│ User clicks "Complete" │
└───────────┬────────────┘
        │
        ▼
┌────────────────────────┐
│ Validate order editable│
│ (Status == Pending)    │
└───────────┬────────────┘
        │
        ▼
┌────────────────────────┐      ┌─────────────────────┐
│ Deduct Stock           │◄─────│ Only if             │
│ (for each item)        │      │ TrackInventory=true │
└───────────┬────────────┘      └─────────────────────┘
        │
        ▼
┌────────────────────────┐
│ Update Order:          │
│ - Status = Completed   │
│ - CompletedAt = now    │
└───────────┬────────────┘
        │
        ▼
    ✅ Done
```

### Cancel Order Flow

```
┌────────────────┐
│ Order Pending  │
└───────┬────────┘
        │
        ▼
┌────────────────────────┐
│ User clicks "Cancel"   │
│ + Enter cancel reason  │
└───────────┬────────────┘
        │
        ▼
┌────────────────────────┐
│ Validate:              │
│ - Status == Pending    │
│ - User has permission  │
│   (Owner OR own order) │
└───────────┬────────────┘
        │
        ▼
┌────────────────────────┐
│ If debt was recorded:  │
│ Reverse debtor balance │
│ CurrentBalance += Debt │
└───────────┬────────────┘
        │
        ▼
┌────────────────────────┐
│ Update Order:          │
│ - Status = Cancelled   │
│ - CancelledAt = now    │
│ - CancelledByUserId    │
│ - CancelReason         │
└───────────┬────────────┘
        │
        ▼
    ✅ Done
```

---

## 6. Payment Flows

### Payment Methods

| Method | Code | Mô tả | Requires Debtor? |
|--------|------|-------|:----------------:|
| Cash | `cash` | Tiền mặt | ❌ |
| Bank | `bank` | Chuyển khoản | ❌ |
| Debt | `debt` | Ghi nợ 100% | ✅ |
| Mixed | `mixed` | Trả một phần, ghi nợ phần còn lại | ✅ |

### Cash/Bank Payment

```csharp
// Đơn giản nhất - không cần debtor
order.PaymentMethod = request.PaymentMethod; // "cash" or "bank"
order.PaidAmount = order.TotalAmount;
order.DebtAmount = 0;
```

### Full Debt Payment

```csharp
// Ghi nợ 100%
if (request.DebtorId == null)
    throw new ValidationException("Debt payment requires a debtor");

order.PaymentMethod = "debt";
order.PaidAmount = 0;
order.DebtAmount = order.TotalAmount;

// Update debtor
var debtor = await _unitOfWork.Debtors.GetByIdAsync(request.DebtorId.Value);
debtor.CurrentBalance -= order.TotalAmount;  // Balance giảm (nợ thêm)
```

### Mixed Payment (Partial + Auto Debt)

```csharp
// Trả một phần, phần còn lại ghi nợ
if (request.DebtorId == null)
    throw new ValidationException("Mixed payment requires a debtor");

if (request.PaidAmount >= order.TotalAmount)
    throw new ValidationException("Paid amount exceeds total - use cash/bank instead");

order.PaymentMethod = "mixed";
order.PaidAmount = request.PaidAmount;
order.DebtAmount = order.TotalAmount - request.PaidAmount;

// Update debtor
var debtor = await _unitOfWork.Debtors.GetByIdAsync(request.DebtorId.Value);
debtor.CurrentBalance -= order.DebtAmount;  // Chỉ ghi nợ phần chưa trả

// Ví dụ:
// TotalAmount = 1,000,000đ
// PaidAmount = 600,000đ (tiền mặt hoặc bank)
// DebtAmount = 400,000đ (ghi nợ)
```

---

## 7. Stock Deduction Rules

### When to Deduct

Stock chỉ bị trừ khi **order được Complete**, KHÔNG trừ khi tạo order.

### Deduction Logic

```csharp
public async Task CompleteOrderAsync(long orderId, Guid userId)
{
    var order = await _unitOfWork.Orders
        .Include(o => o.OrderDetails)
        .GetByIdAsync(orderId);
    
    if (order.Status != OrderStatus.Pending)
        throw new ValidationException("Order is not pending");
    
    // Deduct stock for each item
    foreach (var detail in order.OrderDetails)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
        
        // Chỉ trừ nếu track inventory
        if (product.TrackInventory)
        {
            product.Stock -= detail.Quantity;
            
            // Có thể âm (oversell) - đã warning khi tạo order
            if (product.Stock < 0)
            {
                // Log warning nhưng không block
                _logger.LogWarning($"Product {product.ProductId} stock is negative: {product.Stock}");
            }
        }
    }
    
    order.Status = OrderStatus.Completed;
    order.CompletedAt = DateTime.UtcNow;
    
    await _unitOfWork.SaveChangesAsync();
}
```

### TrackInventory Flag

```csharp
// Không phải product nào cũng cần track inventory
// Ví dụ: Dịch vụ cắt tóc, F&B làm tại chỗ
if (!product.TrackInventory)
{
    // Không trừ stock, chỉ ghi nhận doanh thu
    continue;
}
```

### Negative Stock Handling

- **Không block**: Cho phép stock âm (oversell)
- **Warning khi tạo**: Cảnh báo user trước khi confirm
- **Report**: Báo cáo hàng thiếu cho owner

---

## 8. API Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/orders` | Tạo order mới (manual) |
| `GET` | `/api/orders` | List orders (filter by location, status, date) |
| `GET` | `/api/orders/{id}` | Get order detail |
| `PUT` | `/api/orders/{id}` | Update order (compeletd -> cancelled + clone) |
| `POST` | `/api/orders/{id}/complete` | Mark order as completed |
| `POST` | `/api/orders/{id}/cancel` | Cancel order |
| `POST` | `/api/ai/parse-order` | Parse voice/text to draft order |

---

## Appendix: Error Codes

| Code | HTTP | Message |
|------|------|---------|
| `ORDER_NOT_FOUND` | 404 | Order không tồn tại |
| `ORDER_NOT_EDITABLE` | 400 | Order đã complete/cancel, không thể sửa |
| `PRODUCT_NOT_IN_LOCATION` | 400 | Sản phẩm không thuộc location này |
| `DEBTOR_REQUIRED` | 400 | Debt/Mixed payment cần có debtor |
| `DEBTOR_NOT_FOUND` | 404 | Debtor không tồn tại |
| `NO_PERMISSION` | 403 | Không có quyền thực hiện action này |
| `AI_SERVICE_UNAVAILABLE` | 503 | AI service không khả dụng |
