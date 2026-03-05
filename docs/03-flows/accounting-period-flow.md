# Accounting Period Flow

> **Module**: Report & Accounting — Period Lifecycle
> Quay về [report-accounting-flow.md](report-accounting-flow.md) (index)

---

## Mục lục

1. [Entity Design](#1-entity-design)
   - [1.1 AccountingPeriods](#11-accountingperiods-kỳ-kế-toán)
   - [1.2 AccountingPeriodAuditLogs](#12-accountingperiodauditlogs-lịch-sử-thay-đổi)
2. [Period Lifecycle](#2-period-lifecycle)
   - [2.1 Create Period](#21-create-period)
   - [2.2 Finalize Period](#22-finalize-period)
   - [2.3 Reopen Period](#23-reopen-period)
3. [Business Rules](#3-business-rules)
4. [API Endpoints](#4-api-endpoints)
5. [Error Codes](#5-error-codes)

---

## 1. Entity Design

### 1.1 AccountingPeriods (Kỳ kế toán)

```sql
-- =============================================
-- ACCOUNTING PERIODS TABLE (Kỳ kế toán)
-- =============================================
CREATE TABLE AccountingPeriods (
    PeriodId BIGINT AUTO_INCREMENT PRIMARY KEY,
    BusinessLocationId INT NOT NULL,
    
    -- Period identity
    PeriodType VARCHAR(10) NOT NULL COMMENT 'quarter | year',
    Year SMALLINT NOT NULL,
    Quarter TINYINT DEFAULT NULL COMMENT '1-4 cho quarterly, NULL cho annual',
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    
    -- ═══ OPENING BALANCE (số dư đầu kỳ cho S2e) ═══
    OpeningCashBalance DECIMAL(15,2) DEFAULT NULL 
        COMMENT 'Số dư tiền mặt đầu kỳ. Kỳ đầu tiên: Owner nhập. Kỳ sau: auto carry từ closing kỳ trước.',
    OpeningBankBalance DECIMAL(15,2) DEFAULT NULL 
        COMMENT 'Số dư ngân hàng đầu kỳ. Kỳ đầu tiên: Owner nhập. Kỳ sau: auto carry từ closing kỳ trước.',
    
    -- ═══ LIFECYCLE STATUS ═══
    Status VARCHAR(20) NOT NULL DEFAULT 'open',
    -- 'open':       Đang hoạt động, data thay đổi realtime
    -- 'finalized':  Owner đã chốt kỳ, sổ đã xuất chính thức
    -- 'reopened':   Mở lại sau finalize (cần lý do, audit log)
    
    -- ═══ FINALIZATION ═══
    FinalizedAt DATETIME DEFAULT NULL,
    FinalizedByUserId CHAR(36) DEFAULT NULL,
    
    -- Timestamps
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- Indexes & FKs
    CONSTRAINT fk_period_location FOREIGN KEY (BusinessLocationId) 
        REFERENCES BusinessLocations(BusinessLocationId),
    UNIQUE INDEX idx_period_unique (BusinessLocationId, PeriodType, Year, Quarter),
    INDEX idx_period_status (Status),
    INDEX idx_period_year (Year)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

```csharp
public class AccountingPeriod
{
    public long PeriodId { get; set; }
    public int BusinessLocationId { get; set; }
    
    // Identity
    public string PeriodType { get; set; } = null!;
    public short Year { get; set; }
    public int? Quarter { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    
    // Opening balance (S2e)
    public decimal? OpeningCashBalance { get; set; }
    public decimal? OpeningBankBalance { get; set; }
    
    // Status
    public string Status { get; set; } = "open";
    
    // Finalization
    public DateTime? FinalizedAt { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual ICollection<AccountingBook> AccountingBooks { get; set; } = new List<AccountingBook>();
    public virtual ICollection<AccountingPeriodAuditLog> AuditLogs { get; set; } = new List<AccountingPeriodAuditLog>();
    public virtual ICollection<AccountingExport> Exports { get; set; } = new List<AccountingExport>();
}

public static class PeriodTypes
{
    public const string Quarter = "quarter";
    public const string Year = "year";
}

public static class PeriodStatuses
{
    public const string Open = "open";
    public const string Finalized = "finalized";
    public const string Reopened = "reopened";
}
```

**Lifecycle giải thích:**

```markdown
OPEN ─────────────────────────────────────────────────► FINALIZED
 │  Kỳ đang hoạt động                                  │  Owner chốt kỳ
 │  → GL entries được tạo tự động                      │  → Không block tạo
 │  → Owner tạo/xem AccountingBooks bất kỳ lúc nào     │     GL entries mới
 │  → Owner preview sổ, so sánh nhóm                   │  → Đánh dấu sổ đã
 │  → Chưa chốt → chưa có gì ràng buộc                 │     chính thức
 │                                                     │
 │◄────────────────────────────────────────────────────┘
                        REOPENED
              Mở lại để sửa (bắt buộc có lý do)
              → Audit log ghi nhận
              → Có thể finalize lại sau
```

> **Lưu ý**: `finalized` không block GL entries mới (vì giao dịch vẫn xảy ra). Nó chỉ đánh dấu rằng Owner đã chốt kỳ này và xuất sổ chính thức. Nếu sau đó có thay đổi → book live tự cập nhật → Owner cần export lại nếu muốn bản mới.

### 1.2 AccountingPeriodAuditLogs (Lịch sử thay đổi)

```sql
-- =============================================
-- ACCOUNTING PERIOD AUDIT LOGS TABLE
-- =============================================
CREATE TABLE AccountingPeriodAuditLogs (
    LogId BIGINT AUTO_INCREMENT PRIMARY KEY,
    PeriodId BIGINT NOT NULL,
    
    -- What changed
    Action VARCHAR(50) NOT NULL,
    -- 'period_created'          Tạo kỳ mới
    -- 'period_finalized'        Chốt kỳ
    -- 'period_reopened'         Mở lại kỳ
    -- 'book_created'            Tạo sổ mới
    -- 'book_exported'           Xuất sổ
    -- 'group_suggestion'        Rule Engine gợi ý (system log)
    
    -- Change details
    OldValue JSON DEFAULT NULL COMMENT 'Giá trị trước thay đổi',
    NewValue JSON DEFAULT NULL COMMENT 'Giá trị sau thay đổi',
    Reason TEXT DEFAULT NULL COMMENT 'Lý do (bắt buộc cho reopen)',
    
    -- Audit
    CreatedByUserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes & FKs
    CONSTRAINT fk_audit_period FOREIGN KEY (PeriodId) 
        REFERENCES AccountingPeriods(PeriodId),
    INDEX idx_audit_period (PeriodId),
    INDEX idx_audit_action (Action),
    INDEX idx_audit_date (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

```csharp
public class AccountingPeriodAuditLog
{
    public long LogId { get; set; }
    public long PeriodId { get; set; }
    
    public string Action { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public virtual AccountingPeriod Period { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
}
```

**Ví dụ audit entries:**

```json
// Tạo kỳ mới
{
  "action": "period_created",
  "newValue": { "periodType": "quarter", "year": 2026, "quarter": 1 }
}

// Rule Engine gợi ý (auto log)
{
  "action": "group_suggestion",
  "newValue": {
    "suggestedGroup": 2,
    "actualRevenue": 620000000,
    "annualizedRevenue": 820000000,
    "rulesetVersion": "1.0.0"
  }
}

// Owner tạo sổ theo nhóm 2
{
  "action": "book_created",
  "newValue": { "bookId": 15, "groupNumber": 2, "taxMethod": "method_1", "bookType": "S2a" }
}

// Chốt kỳ
{
  "action": "period_finalized",
  "newValue": { "finalizedAt": "2026-04-10T09:00:00Z" }
}

// Mở lại kỳ
{
  "action": "period_reopened",
  "oldValue": { "status": "finalized" },
  "newValue": { "status": "reopened" },
  "reason": "Phát hiện thiếu 3 đơn hàng chưa complete, cần bổ sung trước khi nộp thuế"
}
```

---

## 2. Period Lifecycle

### 2.1 Create Period

Periods có thể được tạo **tự động** (system job đầu quý/năm) hoặc **thủ công** bởi Owner:

```yaml
POST /api/v1/locations/{locationId}/accounting/periods
Authorization: Bearer {token}

Request:
{
  "periodType": "quarter",
  "year": 2026,
  "quarter": 1,
  "openingCashBalance": 50000000,    # Kỳ đầu tiên: bắt buộc. Kỳ sau: optional (auto carry)
  "openingBankBalance": 120000000    # Kỳ đầu tiên: bắt buộc. Kỳ sau: optional (auto carry)
}

Response (201 Created):
{
  "periodId": 1,
  "periodType": "quarter",
  "year": 2026,
  "quarter": 1,
  "startDate": "2026-01-01",
  "endDate": "2026-03-31",
  "openingCashBalance": 50000000,
  "openingBankBalance": 120000000,
  "status": "open"
}
```

**Auto-carry logic (Kỳ thứ 2 trở đi):**

Nếu Owner không truyền `openingCashBalance` / `openingBankBalance`, hệ thống tự tính từ closing balance kỳ trước:

```csharp
public async Task<AccountingPeriod> CreatePeriodAsync(
    int locationId, Guid userId, CreatePeriodRequest request)
{
    // ... validate unique, date range...
    
    // Auto-carry opening balance từ kỳ trước
    decimal? openingCash = request.OpeningCashBalance;
    decimal? openingBank = request.OpeningBankBalance;
    
    var previousPeriod = await GetPreviousPeriodAsync(locationId, request);
    
    if (previousPeriod == null)
    {
        // Kỳ đầu tiên — bắt buộc nhập
        if (!openingCash.HasValue || !openingBank.HasValue)
            throw new BadRequestException("PERIOD_OPENING_BALANCE_REQUIRED");
    }
    else if (!openingCash.HasValue || !openingBank.HasValue)
    {
        // Kỳ sau — auto carry từ closing balance kỳ trước
        var closing = await _glRepo.CalculateClosingBalanceAsync(
            locationId, previousPeriod.StartDate, previousPeriod.EndDate);
        
        openingCash ??= (previousPeriod.OpeningCashBalance ?? 0) + closing.NetCash;
        openingBank ??= (previousPeriod.OpeningBankBalance ?? 0) + closing.NetBank;
        // NetCash = SUM(Debit WHERE cash) - SUM(Credit WHERE cash) trong kỳ trước
        // NetBank = SUM(Debit WHERE bank) - SUM(Credit WHERE bank) trong kỳ trước
    }
    
    var period = new AccountingPeriod
    {
        BusinessLocationId = locationId,
        PeriodType = request.PeriodType,
        Year = request.Year,
        Quarter = request.Quarter,
        StartDate = CalculateStartDate(request),
        EndDate = CalculateEndDate(request),
        OpeningCashBalance = openingCash,
        OpeningBankBalance = openingBank,
        Status = PeriodStatuses.Open
    };
    
    await _unitOfWork.AccountingPeriods.AddAsync(period);
    await _auditService.LogAsync(period.PeriodId, "period_created", userId, new
    {
        periodType = request.PeriodType, year = request.Year, quarter = request.Quarter,
        openingCashBalance = openingCash, openingBankBalance = openingBank
    });
    
    await _unitOfWork.SaveChangesAsync();
    return period;
}
```
  "quarter": 1,
  "startDate": "2026-01-01",
  "endDate": "2026-03-31",
  "status": "open"
}
```

### 2.2 Finalize Period

```yaml
POST /api/v1/locations/{locationId}/accounting/periods/{periodId}/finalize
Authorization: Bearer {token}

Response:
{
  "periodId": 1,
  "status": "finalized",
  "finalizedAt": "2026-04-10T09:00:00Z",
  "message": "Kỳ Q1/2026 đã được chốt"
}
```

Rules:
- Only Owner
- Phải có ít nhất 1 AccountingBook active trong period
- Ghi audit log

### 2.3 Reopen Period

```yaml
POST /api/v1/locations/{locationId}/accounting/periods/{periodId}/reopen
Authorization: Bearer {token}

Request:
{
  "reason": "Phát hiện thiếu 3 đơn hàng chưa complete"  // Bắt buộc
}

Response:
{
  "periodId": 1,
  "status": "reopened",
  "message": "Kỳ Q1/2026 đã được mở lại"
}
```

Rules:

- Only Owner
- Reason bắt buộc
- Ghi audit log (action = `period_reopened`, oldValue, newValue, reason)

---

## 3. Business Rules

| Rule | Mô tả |
|------|-------|
| **RULE-PERIOD-01** | Unique per location + type + year + quarter |
| **RULE-PERIOD-02** | Finalize bắt buộc có ít nhất 1 book active |
| **RULE-PERIOD-03** | Reopen bắt buộc có reason |
| **RULE-PERIOD-04** | Mọi thay đổi status → audit log |
| **RULE-PERIOD-05** | Kỳ đầu tiên bắt buộc nhập `openingCashBalance` + `openingBankBalance` |
| **RULE-PERIOD-06** | Kỳ thứ 2 trở đi: nếu Owner không truyền → hệ thống auto carry từ closing balance kỳ trước (opening + net GL cash/bank trong kỳ) |
| **RULE-PERIOD-07** | Owner có thể override opening balance bất kỳ kỳ nào (VD: điều chỉnh sau kiểm kê thực tế) |

---

## 4. API Endpoints

| Method | Endpoint | Description | Who |
|--------|----------|-------------|-----|
| `POST` | `/api/v1/locations/{id}/accounting/periods` | Tạo kỳ kế toán | Owner |
| `GET` | `/api/v1/locations/{id}/accounting/periods` | List kỳ kế toán | Owner |
| `GET` | `/api/v1/locations/{id}/accounting/periods/{periodId}` | Chi tiết kỳ | Owner |
| `POST` | `/api/v1/locations/{id}/accounting/periods/{periodId}/finalize` | Chốt kỳ | Owner |
| `POST` | `/api/v1/locations/{id}/accounting/periods/{periodId}/reopen` | Mở lại kỳ | Owner |
| `GET` | `/api/v1/locations/{id}/accounting/periods/{periodId}/audit-logs` | Lịch sử thay đổi | Owner |

---

## 5. Error Codes

| Code | HTTP | Message |
|------|:----:|---------|
| `PERIOD_NOT_FOUND` | 404 | Kỳ kế toán không tồn tại |
| `PERIOD_ALREADY_EXISTS` | 409 | Kỳ kế toán đã tồn tại (trùng year+quarter) |
| `PERIOD_NOT_OPEN` | 400 | Kỳ kế toán không ở trạng thái open |
| `PERIOD_ALREADY_FINALIZED` | 400 | Kỳ kế toán đã chốt rồi |
| `PERIOD_REOPEN_REASON_REQUIRED` | 400 | Phải nhập lý do khi mở lại kỳ |
| `PERIOD_NO_BOOKS` | 400 | Phải có ít nhất 1 sổ active trước khi chốt kỳ |
| `PERIOD_OPENING_BALANCE_REQUIRED` | 400 | Kỳ đầu tiên bắt buộc nhập số dư đầu kỳ (tiền mặt + ngân hàng) |
| `NO_PERMISSION` | 403 | Không có quyền thực hiện action này |
