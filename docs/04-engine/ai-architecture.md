# AI Service Architecture

## 1. Overview

The BizFlow AI Service is an independent Python microservice deployed alongside the .NET Core API on the VPS (Docker Compose). It provides four AI-powered capabilities that help household business owners understand and manage their business without requiring any data science expertise.

| Capability | User-facing description |
|---|---|
| **Voice-to-Draft-Order** | Owner speaks an order naturally; system creates a draft for review |
| **Revenue Forecasting** | Predicts next 7–30 days of revenue based on historical patterns |
| **Anomaly Detection** | Alerts when revenue or accounting figures are unusually high/low or appear erroneous |
| **Reorder Suggestions** | Recommends which products to import, when, and how much |

---

## 2. Technology Stack

| Component | Technology | Reason |
|---|---|---|
| API Framework | **FastAPI** (Python) | Async, fast, auto-generates OpenAPI docs |
| Speech-to-Text | **OpenAI Whisper** / **Google Cloud STT** | Whisper: offline-capable, accurate with Vietnamese; Google STT: higher accuracy with streaming |
| LLM | **OpenAI GPT-4o** / **Google Gemini** | Natural language understanding for order extraction |
| Vector Store | **ChromaDB** + **text-embedding-3-small** | In-process vector DB, no separate service needed; stores product catalog embeddings for RAG |
| Revenue Forecasting | **Facebook Prophet** | Handles Vietnamese holidays, weekly/yearly seasonality, missing data gracefully — designed for business time series |
| Anomaly Detection | **Isolation Forest** (scikit-learn) | Best-in-class for tabular financial anomaly detection; no labels required (unsupervised) |
| Reorder Suggestions | **Pandas** + statistical reorder-point formula | Sales velocity + safety stock model; explainable and verifiable by non-technical users |
| DB Access | **SQLAlchemy** + **PyMySQL** | Reads historical sales/import data from the same Aiven MySQL |
| Scheduler trigger | **Hangfire** (in .NET API) | Scheduled jobs (nightly) call AI Service HTTP endpoints to re-run forecasts, anomaly checks, and reorder calculations |

---

## 3. Service Architecture

### 3.1 Package Structure

```
bizflow-ai/
├── main.py                         FastAPI application entry point, router registration
│
├── routers/
│   ├── draft_order.py              POST /draft-order      (sync — user waits)
│   ├── forecast.py                 POST /forecast         (called by Hangfire scheduler)
│   ├── anomaly.py                  POST /anomaly          (called by Hangfire scheduler)
│   └── reorder.py                  POST /reorder          (called by Hangfire scheduler)
│
├── services/
│   ├── draft_order_service.py      Orchestrates STT → RAG → LLM → structured order
│   ├── forecast_service.py         Loads sales data → runs Prophet → writes results to DB
│   ├── anomaly_service.py          Loads financial data → runs Isolation Forest → writes alerts to DB
│   └── reorder_service.py          Calculates reorder points → writes suggestions to DB
│
├── ml/
│   ├── stt.py                      Wrapper: Whisper or Google STT, returns transcript text
│   ├── llm.py                      Wrapper: OpenAI / Gemini client, prompt templates
│   └── vector_store.py             ChromaDB client: sync product catalog, query similar products
│
├── db/
│   └── mysql_client.py             SQLAlchemy engine, query helpers for reading/writing MySQL
│
├── Dockerfile
└── requirements.txt
```

### 3.2 Communication Pattern with BizFlow API

The AI Service is **not exposed to the internet**. Only the BizFlow API (container-to-container via Docker internal network) can call it.

Two communication patterns are used:

```
Pattern A — Synchronous (used by: Draft Order only)
─────────────────────────────────────────────────────
Client (Flutter/Web)
  → POST /api/orders/draft-order          (sends audio file)
  → BizFlow API                           (forwards to AI Service)
  → AI Service: STT + RAG + LLM          (processes ~5–15 s)
  ← Returns draft order JSON
  ← BizFlow API returns to client
  ← Client displays draft for user review

Pattern B — Asynchronous / Scheduled (used by: Forecast, Anomaly, Reorder)
────────────────────────────────────────────────────────────────────────────
Hangfire Job (runs nightly, e.g., 01:00 AM)
  → POST http://bizflow-ai:5000/forecast  (trigger, no user waiting)
  → AI Service: pulls data from MySQL, runs model, writes results back to MySQL
Client (next morning, opens dashboard)
  → GET /api/analytics/forecast
  → BizFlow API reads pre-computed results from MySQL
  ← Returns instantly (no model inference at read time)
```

> **Why async for forecast/anomaly/reorder?**
> These models process weeks or months of historical data. Running them synchronously on a user request would result in 5–60 second loading times. By pre-computing nightly and storing results, the dashboard responds in < 200 ms.

---

## 4. Feature 1: Voice-to-Draft-Order

### 4.1 Description

The owner speaks a natural Vietnamese command — e.g., *"Bán cho anh Ba 5 bao xi măng, ghi nợ"* — and the system creates a structured draft order that the owner reviews and confirms before saving.

### 4.2 Technology

| Step | Technology |
|---|---|
| Audio capture | Flutter microphone package (on device) |
| Speech-to-Text | **Whisper** (`whisper-1` via OpenAI API) or **Google Cloud STT** (Vietnamese locale: `vi-VN`) |
| Product matching | **RAG**: ChromaDB vector search over the location's product catalog |
| Order extraction | **LLM** (GPT-4o / Gemini): structured extraction with product context injected into prompt |
| Output | JSON draft order (productId, quantity, unit, customerId, isDebt) |

### 4.3 RAG Detail

The product catalog of each business location is embedded and stored in ChromaDB:

```
Sync trigger: when owner creates/edits/deletes a product
  → BizFlow API calls POST /ai/vector-store/sync
  → AI Service: re-embeds changed product (name + unit + category)
  → Stores in ChromaDB collection keyed by locationId
```

When extracting an order:
```
User says: "5 bao xi măng cho anh Ba"
  1. STT → "5 bao xi măng cho anh Ba"
  2. ChromaDB query: top-3 similar products to "xi măng" (from location's catalog)
     → returns: [{"id": 42, "name": "Xi măng Hà Tiên bao 50kg", "unit": "bao", ...}]
  3. LLM prompt (with product context injected):
     → "Given products: [list]... extract order from: '5 bao xi măng cho anh Ba'"
     → returns: {"productId": 42, "quantity": 5, "unit": "bao", "customerName": "Anh Ba", "isDebt": true}
```

### 4.4 Data Flow Diagram

```
[Flutter] ─── audio (multipart) ──► [BizFlow API]
                                          │
                                          │ HTTP POST /draft-order (audio file)
                                          ▼
                                    [AI Service]
                                          │
                          ┌───────────────┼───────────────┐
                          ▼               ▼               ▼
                       [STT]          [ChromaDB]       [LLM]
                    (transcript)   (product context)  (extraction)
                          └───────────────┼───────────────┘
                                          ▼
                                   Draft Order JSON
                                          │
                          ◄───────────────┘
                    [BizFlow API]
                          │
                ◄─────────┘
           [Flutter] displays draft → user confirms → saved to MySQL
```

### 4.5 Fallback

If the AI Service is unavailable (timeout / error), the client falls back to manual order entry. The BizFlow API returns `503 AI Service Unavailable` with a user-friendly message.

---

## 5. Feature 2: Revenue Forecasting

### 5.1 Description

Predicts the business location's revenue for the next **7 days** (default) or up to **30 days**, displayed as a chart on the dashboard. Helps owners anticipate slow/busy periods and plan inventory accordingly.

### 5.2 Technology: Facebook Prophet

Prophet is chosen because:
- Designed specifically for business time-series with **weekly and yearly seasonality**
- Handles **missing days** (e.g., owner was sick, no sales) without special preprocessing
- Supports **Vietnamese public holiday effects** (custom `holidays` dataframe)
- Produces **uncertainty intervals** (upper/lower band) — owner can see the range, not just a single number
- Produces interpretable components: trend + weekly pattern + holiday effects

### 5.3 Input Data

Pulled from MySQL on each scheduled run:

```sql
SELECT DATE(created_at) AS ds, SUM(total_amount) AS y
FROM orders
WHERE location_id = :location_id
  AND status = 'CONFIRMED'
  AND created_at >= DATE_SUB(NOW(), INTERVAL 6 MONTH)
GROUP BY DATE(created_at)
ORDER BY ds
```

> Minimum viable history: **60 days** of confirmed sales. If a location has less data, the forecast is skipped and a "not enough data" flag is stored.

### 5.4 Output

Stored in MySQL table `ai_revenue_forecasts`:

| Column | Type | Description |
|---|---|---|
| `location_id` | UUID | The business location |
| `forecast_date` | DATE | Predicted date |
| `predicted_revenue` | DECIMAL | Point forecast (yhat) |
| `lower_bound` | DECIMAL | 80% confidence lower (yhat_lower) |
| `upper_bound` | DECIMAL | 80% confidence upper (yhat_upper) |
| `generated_at` | DATETIME | Timestamp of forecast run |

### 5.5 Schedule

```
Hangfire RecurringJob: "forecast-all-locations"
  Cron: "0 1 * * *"   (01:00 AM daily)
  → POST http://bizflow-ai:5000/forecast
     body: { "location_ids": [...all active locations...] }
  → AI Service runs Prophet per location, writes to ai_revenue_forecasts
```

### 5.6 Data Flow Diagram

```
[Hangfire Job @ 01:00 AM]
       │ POST /forecast (locationIds[])
       ▼
 [AI Service]
       │
       ├─── SELECT sales data ──► [Aiven MySQL]
       │         ◄── returns historical daily revenue
       │
       ├─── Run Prophet model per location
       │         → 7-day forecast + confidence intervals
       │
       └─── INSERT forecast results ──► [Aiven MySQL ai_revenue_forecasts]

[Next morning: User opens dashboard]
       │ GET /api/analytics/forecast?locationId=...
       ▼
 [BizFlow API]
       │ SELECT FROM ai_revenue_forecasts ──► [Aiven MySQL]
       ◄── returns pre-computed results instantly
       ▼
 [Client renders forecast chart]
```

---

## 6. Feature 3: Anomaly Detection

### 6.1 Description

Monitors the business's financial data and raises alerts when something looks abnormal. Targets two types of anomalies:

| Type | Examples |
|---|---|
| **Revenue anomaly** | A day with near-zero revenue (unexpected closure or entry error), a spike 3× the normal average (data entry mistake), sudden drop in a usually profitable week |
| **Accounting / data quality anomaly** | Product sold at ₫0, an import with total cost far below unit price × quantity, a debt recorded for an unknown customer  |

### 6.2 Technology: Isolation Forest (scikit-learn)

Chosen because:
- **Unsupervised** — no labelled "anomaly" data needed; it learns from the normal patterns
- Efficient on small tabular datasets (household business scale)
- Produces an **anomaly score** (−1 = anomaly, 1 = normal) per data point
- Explainable: the **feature values** that triggered the alert can be shown to the user

### 6.3 Input Features

For revenue anomaly detection (per-day, per-location):

| Feature | Description |
|---|---|
| `revenue` | Total confirmed order revenue that day |
| `order_count` | Number of confirmed orders |
| `avg_order_value` | revenue / order_count |
| `day_of_week` | 0–6 (captures weekly pattern) |
| `is_holiday` | 1 if Vietnamese public holiday |

For accounting data quality:

| Feature | Description |
|---|---|
| `unit_price` | Recorded sale price |
| `expected_price` | Average historical price for this product |
| `price_deviation_ratio` | unit_price / expected_price |
| `quantity` | Units sold |
| `import_cost_per_unit` | For imports: cost / quantity |

### 6.4 Output

Stored in MySQL table `ai_anomaly_alerts`:

| Column | Type | Description |
|---|---|---|
| `location_id` | UUID | |
| `alert_type` | ENUM | `REVENUE_ANOMALY`, `DATA_QUALITY` |
| `severity` | ENUM | `WARNING`, `CRITICAL` |
| `reference_date` | DATE | The date of the anomalous record |
| `description` | TEXT | Human-readable explanation (generated by LLM from anomaly features) |
| `reference_id` | UUID | FK to the specific order/import that triggered the alert |
| `is_acknowledged` | BOOLEAN | Owner has seen/dismissed the alert |
| `generated_at` | DATETIME | |

The `description` field is generated by a brief LLM call:
```
Prompt: "A data anomaly was detected. Features: [revenue=0, order_count=5, ...].
         Write a 1-sentence Vietnamese explanation for a small business owner."
→ "Hệ thống phát hiện ngày 04/03 có 5 đơn hàng nhưng doanh thu ghi nhận bằng 0đ — 
   có thể có lỗi nhập liệu."
```

### 6.5 Schedule

```
Hangfire RecurringJob: "anomaly-check-all-locations"
  Cron: "0 2 * * *"   (02:00 AM daily)
  → POST http://bizflow-ai:5000/anomaly
  → AI Service detects anomalies, writes alerts to ai_anomaly_alerts
  → BizFlow API reads alerts → triggers Firebase FCM notification to owner
     if new CRITICAL alerts found
```

### 6.6 Data Flow Diagram

```
[Hangfire Job @ 02:00 AM]
       │ POST /anomaly (locationIds[])
       ▼
 [AI Service]
       │
       ├─── SELECT recent orders/imports ──► [Aiven MySQL]
       │
       ├─── Build feature matrix (pandas DataFrame)
       │
       ├─── Run Isolation Forest
       │         → rows with anomaly_score = -1
       │
       ├─── LLM: generate Vietnamese description per alert
       │
       └─── INSERT anomaly alerts ──► [Aiven MySQL ai_anomaly_alerts]
                    │
                    ▼
       [BizFlow API checks for new CRITICAL alerts]
                    │ if found: send FCM push notification
                    ▼
       [Firebase FCM] ──► [Flutter/ReactJS: "⚠️ Phát hiện bất thường trong sổ sách"]
```

---

## 7. Feature 4: Reorder / Import Suggestions

### 7.1 Description

Tells the business owner which products are running low and how much to order, based on:
- **Current stock level** (quantity on hand)
- **Sales velocity** (average units sold per day, calculated from historical orders)
- **Optional seasonality** (detected by Prophet: if a product sells 2× more in a certain month, factor that in)
- **Lead time** (fixed assumption: 3 days, configurable per product in future)

Example output: *"Xi măng Hà Tiên: còn 10 bao, dự kiến hết sau 2 ngày dựa trên doanh số. Đề xuất nhập thêm 50 bao."*

### 7.2 Technology

| Component | Technology |
|---|---|
| Sales velocity | **Pandas**: rolling 14-day average of units sold per product per day |
| Seasonality factor | **Prophet** seasonal component (reused from forecast job) |
| Stock level | Read from `products.stock_quantity` in MySQL |
| Reorder logic | Statistical **reorder-point formula** (see below) |

**Reorder-point formula:**

$$\text{Reorder Point} = \text{Avg Daily Sales} \times \text{Lead Time} + \text{Safety Stock}$$

$$\text{Safety Stock} = Z \times \sigma_{\text{daily sales}} \times \sqrt{\text{Lead Time}}$$

Where:
- $Z = 1.65$ (95% service level — reasonable for household business)
- $\sigma$ = standard deviation of daily sales over past 30 days
- Lead time = 3 days (default)

**Suggested order quantity:**

$$\text{Order Qty} = \text{Max Stock Level} - \text{Current Stock}$$

Max Stock Level = 30-day supply at average sales velocity (configurable).

### 7.3 Output

Stored in MySQL table `ai_reorder_suggestions`:

| Column | Type | Description |
|---|---|---|
| `location_id` | UUID | |
| `product_id` | UUID | |
| `current_stock` | DECIMAL | Stock at time of calculation |
| `days_until_stockout` | INT | Estimated days before stock hits zero |
| `suggested_quantity` | DECIMAL | Units to reorder |
| `avg_daily_sales` | DECIMAL | 14-day rolling average |
| `urgency` | ENUM | `LOW` (>7 days), `MEDIUM` (3–7 days), `HIGH` (<3 days) |
| `generated_at` | DATETIME | |

### 7.4 Schedule

```
Hangfire RecurringJob: "reorder-suggestions-all-locations"
  Cron: "0 3 * * *"   (03:00 AM daily)
  → POST http://bizflow-ai:5000/reorder
  → AI Service calculates for all products at all active locations
  → Writes to ai_reorder_suggestions
  → BizFlow API checks for HIGH urgency items → FCM push to owner if any found
```

### 7.5 Data Flow Diagram

```
[Hangfire Job @ 03:00 AM]
       │ POST /reorder (locationIds[])
       ▼
 [AI Service]
       ├─── SELECT current stock per product ──► [Aiven MySQL]
       ├─── SELECT order line items (last 30 days) ──► [Aiven MySQL]
       │
       ├─── Calculate avg daily sales per product (pandas rolling mean)
       ├─── Apply seasonality factor (from cached Prophet model)
       ├─── Calculate reorder point + suggested quantity
       │
       └─── UPSERT suggestions ──► [Aiven MySQL ai_reorder_suggestions]
                    │
       [BizFlow API checks HIGH urgency items]
                    │ if found: FCM push notification
                    ▼
       [Flutter/ReactJS: "📦 3 sản phẩm sắp hết hàng"]
```

---

## 8. Nightly Job Schedule Summary

All scheduled jobs are triggered from **Hangfire** (running inside the .NET API process). They call the AI Service over the Docker internal network.

| Time | Job | AI Service Endpoint |
|---|---|---|
| 01:00 AM | Revenue forecast update | `POST /forecast` |
| 02:00 AM | Anomaly detection | `POST /anomaly` |
| 03:00 AM | Reorder suggestions | `POST /reorder` |

ChromaDB (product vector store) is updated in real-time — synced whenever a product is created, updated, or deleted via `POST /vector-store/sync`.

---

## 9. AI Service Endpoints Summary

| Method | Path | Pattern | Triggered by |
|---|---|---|---|
| `POST` | `/draft-order` | Sync | Client (via BizFlow API proxy) |
| `POST` | `/vector-store/sync` | Sync | BizFlow API (on product change) |
| `POST` | `/forecast` | Async (no wait) | Hangfire job |
| `POST` | `/anomaly` | Async (no wait) | Hangfire job |
| `POST` | `/reorder` | Async (no wait) | Hangfire job |

---

## 10. Database Tables Added by AI Features

```sql
-- Pre-computed revenue forecasts
CREATE TABLE ai_revenue_forecasts (
    id           CHAR(36)       PRIMARY KEY,
    location_id  CHAR(36)       NOT NULL,
    forecast_date DATE          NOT NULL,
    predicted_revenue DECIMAL(15,2),
    lower_bound   DECIMAL(15,2),
    upper_bound   DECIMAL(15,2),
    generated_at  DATETIME      NOT NULL,
    INDEX idx_location_date (location_id, forecast_date)
);

-- Anomaly alerts raised by the AI
CREATE TABLE ai_anomaly_alerts (
    id              CHAR(36)   PRIMARY KEY,
    location_id     CHAR(36)   NOT NULL,
    alert_type      ENUM('REVENUE_ANOMALY','DATA_QUALITY') NOT NULL,
    severity        ENUM('WARNING','CRITICAL') NOT NULL,
    reference_date  DATE,
    description     TEXT       NOT NULL,
    reference_id    CHAR(36),
    is_acknowledged BOOLEAN    DEFAULT FALSE,
    generated_at    DATETIME   NOT NULL,
    INDEX idx_location_severity (location_id, severity, is_acknowledged)
);

-- Reorder suggestions per product
CREATE TABLE ai_reorder_suggestions (
    id                 CHAR(36)   PRIMARY KEY,
    location_id        CHAR(36)   NOT NULL,
    product_id         CHAR(36)   NOT NULL,
    current_stock      DECIMAL(15,3),
    days_until_stockout INT,
    suggested_quantity DECIMAL(15,3),
    avg_daily_sales    DECIMAL(15,3),
    urgency            ENUM('LOW','MEDIUM','HIGH') NOT NULL,
    generated_at       DATETIME   NOT NULL,
    INDEX idx_location_urgency (location_id, urgency)
);
```

---

## 11. Non-Functional Considerations

| Concern | Approach |
|---|---|
| **Draft order latency** | Target < 10 s. Whisper `base` model used first (faster); fallback to `large` only if accuracy is poor. |
| **Prophet model persistence** | Models are retrained nightly from scratch (data is small enough). No model serialisation needed. |
| **Isolation Forest cold start** | Requires minimum 30 data points per location. Locations with less data skip anomaly check. |
| **Reorder cold start** | Requires minimum 14 days of sales history. Locations with less data skip reorder suggestions. |
| **AI Service outage** | All 4 features degrade gracefully: scheduled jobs retry on next run; draft order falls back to manual entry. |
| **ChromaDB data isolation** | Each location's products stored in a separate ChromaDB **collection** (`location_{uuid}`), preventing cross-location data leakage. |
| **Model accuracy transparency** | Forecast charts display confidence intervals (not just a single line) so owners understand predictions are estimates. |
