# Formula Operator Guide

> **Module**: Report & Accounting — Formula Engine
> **Scope**: Giải thích chi tiết các toán tử và cách engine evaluate `ExpressionJson`
> **Liên quan**:
> - [tax-formular-engine.md](tax-formular-engine.md) — Entity design, cache strategy, API endpoints
> - [accounting-book-flow-v2.md](../03-flows/accounting-book-flow-v2.md) — Flow tổng thể sổ kế toán

---

## Mục lục

1. [Tổng quan — Formula Engine là gì?](#1-tổng-quan--formula-engine-là-gì)
2. [Mental model: ExpressionJson là cây AST](#2-mental-model-expressionjson-là-cây-ast)
3. [Thứ tự evaluate (runtime)](#3-thứ-tự-evaluate-runtime)
4. [Danh mục node — bảng tổng hợp nhanh](#4-danh-mục-node--bảng-tổng-hợp-nhanh)
5. [Chi tiết từng node](#5-chi-tiết-từng-node)
   - [5.1 `literal` — Hằng số](#51-literal--hằng-số)
   - [5.2 `ref` — Tham chiếu formula khác](#52-ref--tham-chiếu-formula-khác)
   - [5.3 `aggregate` — Truy vấn dữ liệu thực](#53-aggregate--truy-vấn-dữ-liệu-thực)
   - [5.4 `lookup` — Tra cứu bảng phụ](#54-lookup--tra-cứu-bảng-phụ)
   - [5.5 `op` — Phép toán 2 ngôi](#55-op--phép-toán-2-ngôi)
   - [5.6 `fn` — Hàm toán học](#56-fn--hàm-toán-học)
   - [5.7 `foreach` — Lặp theo nhóm ngành](#57-foreach--lặp-theo-nhóm-ngành)
   - [5.8 `context` — Giá trị runtime trong foreach](#58-context--giá-trị-runtime-trong-foreach)
6. [Làm tròn (Rounding)](#6-làm-tròn-rounding)
7. [Ví dụ end-to-end thực tế](#7-ví-dụ-end-to-end-thực-tế)
8. [Quy tắc khi sửa formula](#8-quy-tắc-khi-sửa-formula)
9. [Các lỗi thường gặp](#9-các-lỗi-thường-gặp)
10. [Checklist review nhanh](#10-checklist-review-nhanh)

---

## 1. Tổng quan — Formula Engine là gì?

**Formula Engine** là module tính toán tự động các ô "computed" trong sổ kế toán TT152. Thay vì hard-code công thức tính thuế vào code C#, mỗi công thức được lưu dưới dạng **JSON có cấu trúc** (gọi là `ExpressionJson`) trong bảng `FormulaDefinitions`.

Engine nhận vào:
- `FormulaEvaluationContext`: chứa `BusinessLocationId`, `PeriodStart`, `PeriodEnd`, `PeriodId`, `RulesetId`, `BusinessTypeIds`
- Danh sách `FormulaDefinition` (đã sort theo `CalculationOrder`)

Engine trả ra:
- `Dictionary<string, decimal>`: key = `Formula.Code`, value = kết quả tính

```
ExpressionJson (JSON trong DB)
        │
        ▼
  FormulaEngine.EvaluateFormulasAsync()
        │
        ├─ Đọc từng formula theo CalculationOrder
        ├─ Parse JSON → cây AST
        ├─ Evaluate đệ quy từ node lá lên node gốc
        │     ├─ literal    → trả về số cố định
        │     ├─ ref        → tra bảng resolved đã tính
        │     ├─ aggregate  → truy vấn DB (Revenues, Costs, GL, Stock)
        │     ├─ lookup     → tra cứu AccountingPeriods / IndustryTaxRates
        │     ├─ op         → tính toán 2 nhánh đệ quy
        │     ├─ fn         → tính toán n args đệ quy
        │     ├─ foreach    → lặp theo BusinessTypeId, reduce
        │     └─ context    → giá trị runtime trong foreach
        ├─ Áp RoundingMode + RoundingPrecision
        └─ Lưu vào results["FORMULA_CODE"] = value
```

---

## 2. Mental model: ExpressionJson là cây AST

`ExpressionJson` là một **cây biểu thức** (Abstract Syntax Tree). Mỗi JSON object là một **node**. Node có thể chứa node con (đệ quy).

Engine evaluate **từ dưới lên** (bottom-up): node lá được tính trước, kết quả truyền lên node cha.

**Ví dụ — cây đơn giản:**

```
SUBTRACT
├── ref "S2C_TOTAL_REVENUE"   → 620,000,000
└── ref "S2C_TOTAL_COST"      → 540,000,000
                                ─────────────
                                    80,000,000
```

JSON tương ứng:

```json
{
  "op": "SUBTRACT",
  "left":  { "ref": "S2C_TOTAL_REVENUE" },
  "right": { "ref": "S2C_TOTAL_COST" }
}
```

**Ví dụ — cây lồng nhau (tính thuế TNCN):**

```
MULTIPLY
├── MAX
│   ├── literal 0
│   └── ref "S2C_PROFIT"           → 80,000,000
│   MAX(0, 80,000,000)             → 80,000,000
└── lookup IndustryTaxRates.TaxRate (TaxType=PIT_M1) → 0.005
                                     ──────────────────────
                                        400,000
```

---

## 3. Thứ tự evaluate (runtime)

### 3.1 Giữa các formula — theo `CalculationOrder`

Engine duyệt các formula theo thứ tự tăng dần `CalculationOrder` trong `TemplateFieldMappings`.

```
Order 1: S2C_TOTAL_REVENUE  ← tính trước (aggregate từ DB)
Order 2: S2C_TOTAL_COST     ← tính trước (aggregate từ DB)
Order 3: S2C_PROFIT         ← ref Order 1, ref Order 2 → OK
Order 4: S2C_PIT            ← ref Order 3 → OK
```

> **Quy tắc**: Formula ở order N **chỉ được** `ref` đến formula có order < N.
> Formula lỗi sẽ được gán `0` và engine **tiếp tục** chạy các formula còn lại (không dừng toàn bộ).

### 3.2 Trong một formula — đệ quy từ lá lên gốc

```
1. Gặp "literal"    → trả về số ngay
2. Gặp "ref"        → tra bảng resolved đã tính
3. Gặp "aggregate"  → query DB → trả về số
4. Gặp "lookup"     → query bảng phụ → trả về số
5. Gặp "op"         → evaluate left, evaluate right → tính
6. Gặp "fn"         → evaluate từng arg → tính
7. Gặp "foreach"    → load nhóm, lặp, reduce
8. Gặp "context"    → đọc runtime state của foreach
```

### 3.3 Sau khi có kết quả thuần

```
1. Áp RoundingMode  (floor / ceil / round_half_up)
2. Áp RoundingPrecision (số chữ số thập phân)
3. Cast theo ResultDataType (decimal / integer)
4. Lưu vào results[formula.Code]
```

---

## 4. Danh mục node — bảng tổng hợp nhanh

| Node | Nhận diện qua key | Dùng khi nào | Kết quả |
|------|-------------------|--------------|---------|
| `literal` | `"literal": <number>` | Hằng số cố định | Số đó |
| `ref` | `"ref": "<CODE>"` | Tham chiếu kết quả formula khác | Giá trị đã tính |
| `aggregate` | `"aggregate": "SUM/AVG/COUNT"` | Tổng hợp từ dữ liệu thực (Revenues, Costs...) | Số thực từ DB |
| `lookup` | `"lookup": { "entity": "..." }` | Tra cứu giá trị từ bảng phụ | Một giá trị scalar |
| `op` | `"op": "ADD/SUBTRACT/..."` | Phép tính nhị phân | Kết quả phép tính |
| `fn` | `"fn": "MAX/MIN/ABS"` | Hàm toán học | Kết quả hàm |
| `foreach` | `"foreach": "revenues/costs"` | Lặp theo ngành kinh doanh | Tổng hợp nhiều nhóm |
| `context` | `"context": "<key>"` | Lấy giá trị runtime bên trong foreach | Số từ context |

---

## 5. Chi tiết từng node

---

### 5.1 `literal` — Hằng số

**Cấu trúc:**

```json
{ "literal": <number> }
```

**Ý nghĩa:** Trả về đúng giá trị đó, không phụ thuộc DB hay context.

**Các trường hợp dùng:**

| Mục đích | Ví dụ |
|----------|-------|
| Ngưỡng miễn thuế | `{ "literal": 500000000 }` — 500 triệu đồng |
| Hệ số cố định | `{ "literal": 0.01 }` — 1% |
| Giá trị fallback bảo vệ âm | `{ "literal": 0 }` — dùng với MAX |
| Số lượng chu kỳ | `{ "literal": 4 }` — 4 quý |

**Ví dụ thực tế — kiểm tra ngưỡng miễn thuế:**

```json
{
  "fn": "MAX",
  "args": [
    { "literal": 0 },
    {
      "op": "SUBTRACT",
      "left":  { "ref": "S2C_TOTAL_REVENUE" },
      "right": { "literal": 500000000 }
    }
  ]
}
```

Giải thích: Nếu doanh thu ≤ 500 triệu → phần chịu thuế = 0. Nếu > 500 triệu → lấy phần vượt.

---

### 5.2 `ref` — Tham chiếu formula khác

**Cấu trúc:**

```json
{ "ref": "<FormulaDefinitions.Code>" }
```

**Ý nghĩa:** Lấy kết quả của một formula đã được tính trước đó (tra trong bảng `resolved`). Nếu code không tồn tại → trả về `0`.

**Quy tắc bắt buộc:**
- Formula được ref phải có `CalculationOrder` **nhỏ hơn** formula hiện tại.
- `Code` phải khớp chính xác (case-sensitive).

**Ví dụ:**

```json
{ "ref": "S2C_TOTAL_REVENUE" }
{ "ref": "S2C_TOTAL_COST" }
{ "ref": "S2D_OPENING_STOCK_VALUE" }
```

**Chuỗi phụ thuộc thực tế trong S2c (Bảng kê hàng hóa dịch vụ):**

```
Order 1: S2C_TOTAL_REVENUE   = SUM(revenues.Amount)
Order 2: S2C_TOTAL_COST      = SUM(costs.Amount)
Order 3: S2C_PROFIT          = ref(S2C_TOTAL_REVENUE) - ref(S2C_TOTAL_COST)
Order 4: S2C_PIT             = MAX(0, ref(S2C_PROFIT)) × TaxRate(PIT_M1)
```

---

### 5.3 `aggregate` — Truy vấn dữ liệu thực

**Cấu trúc đầy đủ:**

```json
{
  "aggregate": "<aggType>",
  "source":    "<source>",
  "field":     "<field>",
  "filter":    { "<key>": "<value>" | ["<v1>", "<v2>"] },
  "periodFilter": "<periodFilter>",
  "sign":      "<sign>"
}
```

#### Trường `aggregate` — Phép gộp

| Giá trị | Ý nghĩa | Khi không có dữ liệu |
|---------|---------|----------------------|
| `"SUM"` | Tổng cộng | `0` |
| `"AVG"` | Trung bình | `0` |
| `"COUNT"` | Đếm số lượng bản ghi | `0` |

#### Trường `source` — Nguồn dữ liệu

| Giá trị | Bảng dữ liệu | Mô tả |
|---------|-------------|-------|
| `"revenues"` | `Revenues` | Doanh thu bán hàng, doanh thu thủ công |
| `"costs"` | `Costs` | Chi phí phát sinh trong kỳ |
| `"gl_entries"` | `GeneralLedgerEntries` | Bút toán sổ cái (tiền mặt, ngân hàng) |
| `"stock_movements"` | `StockMovements` | Biến động tồn kho (nhập/xuất/điều chỉnh) |

#### Trường `field` — Trường cần tính

Tùy theo `source`:

**source = `"revenues"`**

| Field | Ý nghĩa |
|-------|---------|
| `"Amount"` | Số tiền doanh thu |

**source = `"costs"`**

| Field | Ý nghĩa |
|-------|---------|
| `"Amount"` | Số tiền chi phí |

**source = `"gl_entries"`**

| Field | Ý nghĩa |
|-------|---------|
| `"DebitAmount"` | Số tiền bên Nợ |
| `"CreditAmount"` | Số tiền bên Có |

**source = `"stock_movements"`**

| Field | Ý nghĩa |
|-------|---------|
| `"Quantity"` hoặc `"QuantityDelta"` | Số lượng biến động (âm = xuất, dương = nhập) |
| `"TotalValue"` | Giá trị = `|Quantity| × giá vốn`. Engine tự tra bảng `Imports` để lấy giá nhập đúng cho từng lô hàng |

#### Trường `filter` — Bộ lọc bổ sung

Giá trị có thể là string đơn hoặc array (OR logic):

| `source` | Filter key hợp lệ | Ví dụ |
|----------|------------------|-------|
| `revenues` | `RevenueType` | `["sale", "manual"]` |
| `gl_entries` | `MoneyChannel` | `"cash"` hoặc `"bank"` |
| `gl_entries` | `TransactionType` | `"income"` hoặc `"expense"` |
| `stock_movements` | `ProductId` | `"prod-001"` |

#### Trường `periodFilter` — Phạm vi kỳ

| Giá trị | Ý nghĩa | Dùng cho |
|---------|---------|---------|
| `"current"` (mặc định) | Trong kỳ: `PeriodStart ≤ date ≤ PeriodEnd` | Doanh thu, chi phí trong quý |
| `"before"` | Trước kỳ: `date < PeriodStart` | Tồn kho đầu kỳ |
| (không có) | `"current"` | — |

#### Trường `sign` — Lọc theo dấu

| Giá trị | Ý nghĩa | Dùng cho |
|---------|---------|---------|
| `"positive"` | Chỉ lấy giá trị > 0 | Nhập kho |
| `"negative"` | Chỉ lấy giá trị < 0 | Xuất kho |
| (không có) | Lấy tất cả | — |

> `sign` được áp dụng **in-memory** sau khi DB đã lọc theo `periodFilter` và `filter`.

#### Ví dụ thực tế

**Tổng doanh thu bán hàng trong kỳ:**

```json
{
  "aggregate": "SUM",
  "source": "revenues",
  "field": "Amount",
  "filter": { "RevenueType": ["sale", "manual"] }
}
```

**Tổng tiền mặt thu vào (GL):**

```json
{
  "aggregate": "SUM",
  "source": "gl_entries",
  "field": "DebitAmount",
  "filter": {
    "MoneyChannel": "cash",
    "TransactionType": "income"
  }
}
```

**Số lượng xuất kho trong kỳ (chỉ lấy số âm):**

```json
{
  "aggregate": "SUM",
  "source": "stock_movements",
  "field": "Quantity",
  "periodFilter": "current",
  "sign": "negative"
}
```

**Giá trị tồn kho đầu kỳ (trước kỳ, chỉ nhập = dương):**

```json
{
  "aggregate": "SUM",
  "source": "stock_movements",
  "field": "TotalValue",
  "periodFilter": "before",
  "sign": "positive"
}
```

**Giá trị tồn kho của 1 sản phẩm cụ thể:**

```json
{
  "aggregate": "SUM",
  "source": "stock_movements",
  "field": "TotalValue",
  "periodFilter": "before",
  "filter": { "ProductId": "prod-001" }
}
```

---

### 5.4 `lookup` — Tra cứu bảng phụ

**Cấu trúc:**

```json
{
  "lookup": {
    "entity": "<entity>",
    "field":  "<field>",
    "filter": { "<key>": "<value>" }
  }
}
```

Engine chỉ cho phép tra cứu **2 entity** (whitelist cố định):

#### Entity `AccountingPeriods` — Số dư đầu kỳ

| Field | Ý nghĩa |
|-------|---------|
| `"OpeningCashBalance"` | Số dư tiền mặt đầu kỳ |
| `"OpeningBankBalance"` | Số dư tiền gửi ngân hàng đầu kỳ |

> Không cần `filter` — engine tự dùng `ctx.PeriodId` để xác định kỳ.

**Ví dụ — lấy tồn tiền mặt đầu kỳ:**

```json
{
  "lookup": {
    "entity": "AccountingPeriods",
    "field": "OpeningCashBalance"
  }
}
```

#### Entity `IndustryTaxRates` — Thuế suất theo ngành

| Field | Ý nghĩa |
|-------|---------|
| `"TaxRate"` | Tỷ lệ thuế (dạng số thực, ví dụ: `0.01` = 1%) |

| Filter key | Giá trị | Ý nghĩa |
|------------|---------|---------|
| `TaxType` | `"VAT"` | Thuế GTGT |
| `TaxType` | `"PIT_M1"` | Thuế TNCN phương pháp 1 |
| `TaxType` | `"PIT_M2"` | Thuế TNCN phương pháp 2 |

> Bên trong `foreach`, engine tự thêm điều kiện `BusinessTypeId = ctx.CurrentBusinessTypeId` để lấy đúng thuế suất của ngành đang xét.

**Ví dụ — lấy thuế suất VAT:**

```json
{
  "lookup": {
    "entity": "IndustryTaxRates",
    "field": "TaxRate",
    "filter": { "TaxType": "VAT" }
  }
}
```

**Ví dụ — lấy thuế TNCN phương pháp 1:**

```json
{
  "lookup": {
    "entity": "IndustryTaxRates",
    "field": "TaxRate",
    "filter": { "TaxType": "PIT_M1" }
  }
}
```

> **Edge case**: Nếu không tìm thấy TaxRate phù hợp → trả về `0`. Engine ghi log warning kèm danh sách `TaxType` có sẵn trong DB để debug.

---

### 5.5 `op` — Phép toán 2 ngôi

**Cấu trúc:**

```json
{
  "op":    "<operator>",
  "left":  <node>,
  "right": <node>
}
```

`left` và `right` là bất kỳ node nào (đệ quy). Engine evaluate `left` trước, `right` sau, rồi tính.

#### Các toán tử

| `op` | Phép tính | Kết quả |
|------|-----------|---------|
| `"ADD"` | `left + right` | Tổng |
| `"SUBTRACT"` | `left - right` | Hiệu |
| `"MULTIPLY"` | `left × right` | Tích |
| `"DIVIDE"` | `left ÷ right` | Thương. Nếu `right = 0` → trả `0` (không throw) |

#### Ví dụ thực tế

**Tính chênh lệch doanh thu - chi phí:**

```json
{
  "op": "SUBTRACT",
  "left":  { "ref": "S2C_TOTAL_REVENUE" },
  "right": { "ref": "S2C_TOTAL_COST" }
}
```

**Tính thuế = doanh thu × thuế suất:**

```json
{
  "op": "MULTIPLY",
  "left":  { "ref": "S2A_QUARTERLY_TOTAL" },
  "right": {
    "lookup": {
      "entity": "IndustryTaxRates",
      "field": "TaxRate",
      "filter": { "TaxType": "VAT" }
    }
  }
}
```

**Tính tồn cuối kỳ = tồn đầu + nhập - xuất:**

```json
{
  "op": "SUBTRACT",
  "left": {
    "op": "ADD",
    "left":  { "ref": "S2D_OPENING_QTY" },
    "right": { "ref": "S2D_IMPORT_QTY" }
  },
  "right": { "ref": "S2D_EXPORT_QTY" }
}
```

**Tính đơn giá bình quân gia quyền = (tồn đầu giá trị + nhập giá trị) ÷ (tồn đầu SL + nhập SL):**

```json
{
  "op": "DIVIDE",
  "left": {
    "op": "ADD",
    "left":  { "ref": "S2D_OPENING_VALUE" },
    "right": { "ref": "S2D_IMPORT_VALUE" }
  },
  "right": {
    "op": "ADD",
    "left":  { "ref": "S2D_OPENING_QTY" },
    "right": { "ref": "S2D_IMPORT_QTY" }
  }
}
```

> Nếu tổng số lượng = 0 (không có hàng), `DIVIDE` trả về `0` thay vì lỗi.

---

### 5.6 `fn` — Hàm toán học

**Cấu trúc:**

```json
{
  "fn":   "<function>",
  "args": [ <node1>, <node2>, ... ]
}
```

`args` là mảng node, mỗi phần tử được evaluate độc lập.

#### Các hàm hỗ trợ

| `fn` | Ý nghĩa | Số args | Khi args rỗng |
|------|---------|---------|---------------|
| `"MAX"` | Giá trị lớn nhất trong danh sách | ≥ 1 | `0` |
| `"MIN"` | Giá trị nhỏ nhất trong danh sách | ≥ 1 | `0` |
| `"ABS"` | Giá trị tuyệt đối | 1 | `0` |

#### Ví dụ thực tế

**Bảo vệ giá trị âm — chỉ lấy lợi nhuận dương để tính thuế:**

```json
{
  "fn": "MAX",
  "args": [
    { "literal": 0 },
    { "ref": "S2C_PROFIT" }
  ]
}
```

Giải thích: Nếu `S2C_PROFIT = -50,000,000` (lỗ) → `MAX(0, -50M) = 0` → thuế = 0.
Nếu `S2C_PROFIT = 80,000,000` (lãi) → `MAX(0, 80M) = 80,000,000` → tính thuế bình thường.

**Lấy phần vượt ngưỡng (khoản chịu thuế):**

```json
{
  "fn": "MAX",
  "args": [
    { "literal": 0 },
    {
      "op": "SUBTRACT",
      "left":  { "ref": "S2C_TOTAL_REVENUE" },
      "right": { "literal": 500000000 }
    }
  ]
}
```

Giải thích: Nếu doanh thu ≤ 500 triệu → khoản chịu thuế = 0. Nếu > 500 triệu → lấy phần vượt.

**Lấy giá trị tuyệt đối của chênh lệch:**

```json
{
  "fn": "ABS",
  "args": [
    {
      "op": "SUBTRACT",
      "left":  { "ref": "S2E_CASH_RECEIPT" },
      "right": { "ref": "S2E_CASH_PAYMENT" }
    }
  ]
}
```

---

### 5.7 `foreach` — Lặp theo nhóm ngành

`foreach` là node phức tạp nhất, dùng cho bài toán **HKD kinh doanh nhiều ngành nghề** — mỗi ngành có thuế suất riêng, cần tính thuế riêng rồi cộng lại.

**Cấu trúc đầy đủ:**

```json
{
  "foreach": "<source>",
  "field":   "<field>",
  "apply": <node — biểu thức áp dụng cho từng nhóm>,
  "reduce": "SUM | MAX | MIN",

  "costSource": "<source>",
  "costField":  "<field>",

  "threshold": {
    "min":       <number>,
    "elseValue": <number>
  },

  "deduction": {
    "amount": <number>,
    "target": "highest_revenue"
  }
}
```

#### Luồng hoạt động từng bước

```
1. Load doanh thu group theo BusinessTypeId
   → revenueByGroup = { "bt-retail": 300M, "bt-food": 200M, "bt-service": 100M }

2. (Tuỳ chọn) threshold check:
   totalAmount = 300M + 200M + 100M = 600M
   Nếu totalAmount ≤ threshold.min → trả về threshold.elseValue (không tính nữa)

3. (Tuỳ chọn) deduction:
   Nhóm có doanh thu cao nhất = "bt-retail"
   → ctx.GroupDeduction["bt-retail"] = deduction.amount

4. Lặp từng nhóm:
   - Set ctx.CurrentBusinessTypeId = "bt-retail"
   - Set ctx.GroupAmount = 300M
   - Set ctx.GroupCost = costByGroup["bt-retail"] (nếu có costSource)
   - Set ctx.GroupDeduction = deduction.amount (hoặc 0)
   - Evaluate apply node → kết quả nhóm này
   - Lặp tiếp "bt-food", "bt-service"...

5. Restore context về giá trị gốc (finally block — đảm bảo không leak)

6. Reduce:
   "SUM" → tổng tất cả nhóm
   "MAX" → nhóm có giá trị lớn nhất
   "MIN" → nhóm có giá trị nhỏ nhất
```

#### Các trường chi tiết

| Trường | Bắt buộc | Ý nghĩa |
|--------|----------|---------|
| `foreach` | ✅ | Source để group: `"revenues"` hoặc `"costs"` |
| `field` | ✅ | Field để tính: `"Amount"` |
| `apply` | ✅ | Biểu thức áp dụng cho từng nhóm (có thể dùng `context`) |
| `reduce` | Không (default `SUM`) | Cách gộp kết quả: `SUM` / `MAX` / `MIN` |
| `costSource` | Không | Source chi phí nếu `apply` cần tính lợi nhuận |
| `costField` | Không | Field chi phí (default `"Amount"`) |
| `threshold.min` | Không | Nếu tổng doanh thu ≤ min → skip, trả `elseValue` |
| `threshold.elseValue` | Không (default `0`) | Giá trị trả về khi không vượt ngưỡng |
| `deduction.amount` | Không | Số tiền khấu trừ thêm vào nhóm doanh thu cao nhất |
| `deduction.target` | Không (default `highest_revenue`) | Nhóm nhận deduction |

#### Ví dụ thực tế — Tính tổng thuế khoán nhiều ngành

Tình huống: HKD kinh doanh 3 ngành, mỗi ngành có thuế suất VAT riêng trong `IndustryTaxRates`.

```json
{
  "foreach": "revenues",
  "field": "Amount",
  "reduce": "SUM",
  "apply": {
    "op": "MULTIPLY",
    "left":  { "context": "group_amount" },
    "right": {
      "lookup": {
        "entity": "IndustryTaxRates",
        "field": "TaxRate",
        "filter": { "TaxType": "VAT" }
      }
    }
  }
}
```

Giải thích bước chạy:
```
revenueByGroup = { "bt-retail": 300M, "bt-food": 200M, "bt-service": 100M }

Nhóm "bt-retail":
  ctx.CurrentBusinessTypeId = "bt-retail"
  ctx.GroupAmount = 300M
  lookup VAT cho "bt-retail" → 0.01 (1%)
  300M × 0.01 = 3,000,000

Nhóm "bt-food":
  ctx.CurrentBusinessTypeId = "bt-food"
  ctx.GroupAmount = 200M
  lookup VAT cho "bt-food" → 0.03 (3%)
  200M × 0.03 = 6,000,000

Nhóm "bt-service":
  ctx.CurrentBusinessTypeId = "bt-service"
  ctx.GroupAmount = 100M
  lookup VAT cho "bt-service" → 0.05 (5%)
  100M × 0.05 = 5,000,000

reduce SUM → 3M + 6M + 5M = 14,000,000
```

#### Ví dụ — foreach với threshold (miễn thuế nếu doanh thu ≤ 500 triệu)

```json
{
  "foreach": "revenues",
  "field": "Amount",
  "reduce": "SUM",
  "threshold": {
    "min": 500000000,
    "elseValue": 0
  },
  "apply": {
    "op": "MULTIPLY",
    "left":  { "context": "group_amount" },
    "right": {
      "lookup": {
        "entity": "IndustryTaxRates",
        "field": "TaxRate",
        "filter": { "TaxType": "PIT_M1" }
      }
    }
  }
}
```

Nếu tổng doanh thu = 400 triệu → `400M ≤ 500M` → trả về `0` ngay, không tính từng nhóm.

#### Ví dụ — foreach với deduction (trừ bậc miễn đầu tiên vào nhóm cao nhất)

```json
{
  "foreach": "revenues",
  "field": "Amount",
  "reduce": "SUM",
  "deduction": {
    "amount": 500000000,
    "target": "highest_revenue"
  },
  "apply": {
    "op": "MULTIPLY",
    "left": {
      "fn": "MAX",
      "args": [
        { "literal": 0 },
        {
          "op": "SUBTRACT",
          "left":  { "context": "group_amount" },
          "right": { "context": "group_deduction" }
        }
      ]
    },
    "right": {
      "lookup": {
        "entity": "IndustryTaxRates",
        "field": "TaxRate",
        "filter": { "TaxType": "PIT_M1" }
      }
    }
  }
}
```

Giải thích: Nhóm có doanh thu cao nhất được trừ đi 500 triệu trước khi nhân thuế suất.

---

### 5.8 `context` — Giá trị runtime trong foreach

**Cấu trúc:**

```json
{ "context": "<key>" }
```

Node `context` **chỉ có nghĩa bên trong `apply` của `foreach`**. Bên ngoài foreach → trả về `0`.

#### Các key hợp lệ

| Key | Giá trị | Ý nghĩa |
|-----|---------|---------|
| `"group_amount"` | `ctx.GroupAmount` | Doanh thu của nhóm đang xét trong vòng lặp |
| `"group_cost"` | `ctx.GroupCost` | Chi phí của nhóm đang xét (chỉ có khi foreach có `costSource`) |
| `"group_deduction"` | `ctx.GroupDeduction` | Khoản khấu trừ áp dụng cho nhóm này (từ `deduction.amount`) |
| `"total_amount"` | `ctx.TotalAmount` | Tổng doanh thu của tất cả nhóm |

**Ví dụ — tính thuế trên lợi nhuận từng nhóm:**

```json
{
  "foreach": "revenues",
  "field": "Amount",
  "costSource": "costs",
  "costField": "Amount",
  "reduce": "SUM",
  "apply": {
    "op": "MULTIPLY",
    "left": {
      "fn": "MAX",
      "args": [
        { "literal": 0 },
        {
          "op": "SUBTRACT",
          "left":  { "context": "group_amount" },
          "right": { "context": "group_cost" }
        }
      ]
    },
    "right": {
      "lookup": {
        "entity": "IndustryTaxRates",
        "field": "TaxRate",
        "filter": { "TaxType": "PIT_M1" }
      }
    }
  }
}
```

Giải thích: Với mỗi nhóm ngành, tính `MAX(0, doanhThu - chiPhi) × thueSuat`. Tổng lại = tổng thuế TNCN.

---

## 6. Làm tròn (Rounding)

Sau khi evaluate xong, engine áp dụng làm tròn theo cấu hình của `FormulaDefinition`:

| `RoundingMode` | Công thức C# | Ví dụ (precision=0) |
|----------------|-------------|---------------------|
| `"floor"` | `Math.Floor(value × 10^p) / 10^p` | `1,234.9 → 1,234` |
| `"ceil"` | `Math.Ceiling(value × 10^p) / 10^p` | `1,234.1 → 1,235` |
| `"round_half_up"` | `Math.Round(value, p, MidpointRounding.AwayFromZero)` | `1,234.5 → 1,235` |
| `null` (không set) | Không làm tròn | Giữ nguyên |

`RoundingPrecision` = số chữ số thập phân sau dấu phẩy (0 = làm tròn đến đơn vị).

> **Lưu ý**: Không dùng `fn: "ROUND"` trong ExpressionJson nếu đã set `RoundingMode` ở cấp formula — tránh làm tròn 2 lần.

---

## 7. Ví dụ end-to-end thực tế

### 7.1 Chuỗi công thức S2c — Bảng kê hàng hoá dịch vụ

Giả sử HKD kinh doanh 1 ngành, kỳ tính là Quý 1/2025.

**Dữ liệu DB:**
- `revenues.Amount` trong kỳ = 620,000,000 đ
- `costs.Amount` trong kỳ = 540,000,000 đ
- `IndustryTaxRates`: `PIT_M1 = 0.005` (0.5%)

**Formula 1 — S2C_TOTAL_REVENUE (Order 1):**
```json
{
  "aggregate": "SUM",
  "source": "revenues",
  "field": "Amount"
}
```
Kết quả: `620,000,000`

**Formula 2 — S2C_TOTAL_COST (Order 2):**
```json
{
  "aggregate": "SUM",
  "source": "costs",
  "field": "Amount"
}
```
Kết quả: `540,000,000`

**Formula 3 — S2C_PROFIT (Order 3):**
```json
{
  "op": "SUBTRACT",
  "left":  { "ref": "S2C_TOTAL_REVENUE" },
  "right": { "ref": "S2C_TOTAL_COST" }
}
```
Kết quả: `620,000,000 - 540,000,000 = 80,000,000`

**Formula 4 — S2C_PIT (Order 4):**
```json
{
  "op": "MULTIPLY",
  "left": {
    "fn": "MAX",
    "args": [
      { "literal": 0 },
      { "ref": "S2C_PROFIT" }
    ]
  },
  "right": {
    "lookup": {
      "entity": "IndustryTaxRates",
      "field": "TaxRate",
      "filter": { "TaxType": "PIT_M1" }
    }
  }
}
```
Bước 1: `MAX(0, 80,000,000) = 80,000,000`
Bước 2: `lookup PIT_M1 = 0.005`
Kết quả: `80,000,000 × 0.005 = 400,000`

---

### 7.2 S2d — Bình quân gia quyền xuất kho

**Dữ liệu DB** (sản phẩm "sp-001"):
- Tồn đầu kỳ: 100 cái × 80,000đ = 8,000,000đ
- Nhập trong kỳ: 200 cái × 85,000đ = 17,000,000đ

**Formula S2D_OPENING_QTY:**
```json
{
  "aggregate": "SUM",
  "source": "stock_movements",
  "field": "Quantity",
  "periodFilter": "before",
  "sign": "positive",
  "filter": { "ProductId": "sp-001" }
}
```
Kết quả: `100`

**Formula S2D_OPENING_VALUE:**
```json
{
  "aggregate": "SUM",
  "source": "stock_movements",
  "field": "TotalValue",
  "periodFilter": "before",
  "sign": "positive",
  "filter": { "ProductId": "sp-001" }
}
```
Kết quả: `8,000,000`

**Formula S2D_IMPORT_QTY / S2D_IMPORT_VALUE:** (tương tự với `periodFilter: "current"`)
Kết quả: `200` / `17,000,000`

**Formula S2D_WEIGHTED_AVG:**
```json
{
  "op": "DIVIDE",
  "left": {
    "op": "ADD",
    "left":  { "ref": "S2D_OPENING_VALUE" },
    "right": { "ref": "S2D_IMPORT_VALUE" }
  },
  "right": {
    "op": "ADD",
    "left":  { "ref": "S2D_OPENING_QTY" },
    "right": { "ref": "S2D_IMPORT_QTY" }
  }
}
```
Kết quả: `(8,000,000 + 17,000,000) / (100 + 200) = 25,000,000 / 300 = 83,333đ/cái`

---

### 7.3 S2e — Tồn tiền mặt cuối kỳ

```json
{
  "op": "ADD",
  "left": {
    "op": "SUBTRACT",
    "left": {
      "lookup": {
        "entity": "AccountingPeriods",
        "field": "OpeningCashBalance"
      }
    },
    "right": { "ref": "S2E_CASH_PAYMENT" }
  },
  "right": { "ref": "S2E_CASH_RECEIPT" }
}
```

Giải thích: `Tồn cuối = Tồn đầu kỳ - Tiền chi + Tiền thu`

---

## 8. Quy tắc khi sửa formula

Khi sửa `ExpressionJson`, impact lan theo 2 lớp:

**Lớp 1 — Formula bị sửa trực tiếp:**
- Tất cả ô trong sổ có `FormulaId` trỏ đến formula này sẽ tính lại.

**Lớp 2 — Formula dùng `ref` đến formula vừa sửa:**
- Nếu `S2C_PROFIT` thay đổi → `S2C_PIT` (dùng `ref S2C_PROFIT`) cũng thay đổi.

**Quy trình kiểm tra an toàn:**
1. Preview kết quả hiện tại → ghi lại.
2. Sửa `ExpressionJson`.
3. Preview lại.
4. So sánh: chênh lệch có đúng như kỳ vọng không?
5. Kiểm tra các formula phụ thuộc downstream.

---

## 9. Các lỗi thường gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|-----|-------------|------------|
| `FORMULA_MISSING_REF` | `ref` trỏ đến code không tồn tại hoặc chưa được evaluate | Kiểm tra `Code` và `CalculationOrder` |
| `FORMULA_CIRCULAR_DEP` | A ref B và B ref A | Tách công thức trung gian, sắp xếp lại dependency |
| `FORMULA_ORDER_VIOLATION` | Formula order thấp ref formula order cao | Đổi `CalculationOrder` |
| `FORMULA_AGGREGATE_INVALID_SOURCE` | `source` không thuộc whitelist | Dùng: `revenues`, `costs`, `gl_entries`, `stock_movements` |
| `FORMULA_LOOKUP_DENIED` | `entity` lookup không được phép | Dùng: `AccountingPeriods`, `IndustryTaxRates` |
| Thuế = 0 dù có dữ liệu | `TaxType` trong filter không khớp DB | Kiểm tra log warning "Tax rate not found", đối chiếu TaxType trong `IndustryTaxRates` |
| Tồn kho = 0 | `periodFilter` sai | `"before"` cho tồn đầu kỳ, `"current"` cho biến động trong kỳ |
| Chia mẫu số = 0 | `DIVIDE` khi không có hàng | Engine trả `0` — kiểm tra data đầu vào |

---

## 10. Checklist review nhanh

Trước khi activate template/formula:

- [ ] Formula có `ref` đúng code và đúng `CalculationOrder` chưa?
- [ ] `aggregate.source` + `field` có trong danh sách được phép không?
- [ ] `DIVIDE` đã chấp nhận rule trả `0` khi mẫu số = 0 chưa?
- [ ] Làm tròn: không bị double-round (không dùng `fn ROUND` trong expression nếu đã set `RoundingMode`)?
- [ ] `foreach` có `apply` dùng `context` đúng key chưa (`group_amount`, `group_cost`...)?
- [ ] `lookup IndustryTaxRates`: `TaxType` value trong `filter` có khớp chính xác với giá trị trong DB không?
- [ ] Đã test preview before/after và đọc diff kết quả chưa?
