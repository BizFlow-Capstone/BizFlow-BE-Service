# 📚 Hướng dẫn Database Migration - BizFlow

> Hướng dẫn đơn giản cho việc làm việc với database

---

## 🎯 Các tình huống thường gặp

### 1️⃣ Lần đầu setup project

```bash
# Bước 1: Khởi động Docker
docker-compose up -d

# Bước 2: Đợi 15 giây, sau đó vào thư mục scripts
cd database\scripts

# Bước 3: Chạy 2 file này (double-click hoặc gõ lệnh):
apply_new_migrations.bat    # Tạo database và tables
scaffold_entities.bat        # Tạo code C# từ database

# Bước 4: Build project
cd ..\..\bizflow-platform
dotnet build
```

---

### 2️⃣ Khi pull code mới từ Git (có migration mới)

```bash
# Bước 1: Pull code
git pull

# Bước 2: Vào thư mục scripts và chạy
cd database\scripts
apply_new_migrations.bat    # Cập nhật database
scaffold_entities.bat        # Cập nhật code C#

# Bước 3: Build lại
cd ..\..\bizflow-platform
dotnet build
```

---

### 3️⃣ Khi tạo migration mới (thêm/sửa table)

```bash
# Bước 1: Xem migration cuối cùng là số mấy
cd database\migrations
dir
# Ví dụ thấy: 003_insert_default_roles.sql

# Bước 2: Tạo file mới với số tiếp theo
# Tên file: 004_create_user_table.sql
```

**Nội dung file:**
```sql
-- ================================================================
-- Migration: 004_create_user_table
-- Description: Tạo bảng Users
-- Date: 2026-01-26
-- ================================================================

CREATE TABLE IF NOT EXISTS Users (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    Username VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Track migration
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('004_create_user_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
```

```bash
# Bước 3: Test migration
cd ..\scripts
run_migration.bat ..\migrations\004_create_user_table.sql

# Bước 4: Xem kết quả
view_database.bat

# Bước 5: Nếu OK, commit
git add database/migrations/004_create_user_table.sql
git commit -m "feat(db): add users table"
git push
```

---

### 4️⃣ Xem thông tin database

```bash
cd database\scripts
view_database.bat
```

Sẽ hiển thị:
- Danh sách tables
- Migrations đã chạy
- Số lượng records

---

## 🛠️ Chi tiết các Scripts

### 1. `apply_new_migrations.bat` - Tự động áp dụng migrations mới

**Mô tả:**
- Tự động phát hiện và chạy các migration chưa được áp dụng
- Kiểm tra từ database `__MigrationHistory` (không dùng file txt)
- Chạy theo thứ tự tên file (001, 002, 003...)

**Cách dùng:**
```bash
cd database\scripts
apply_new_migrations.bat
```

**Script sẽ làm gì:**
1. ✅ Kiểm tra Docker container có đang chạy không
2. ✅ Lấy danh sách migrations đã chạy từ bảng `__MigrationHistory`
3. ✅ So sánh với file migrations trong thư mục `database/migrations/`
4. ✅ Hiển thị danh sách migrations mới và hỏi xác nhận
5. ✅ Chạy từng migration theo thứ tự
6. ✅ Mỗi migration tự động insert record vào `__MigrationHistory`

**Output mẫu:**
```
================================================
   AUTO APPLY NEW MIGRATIONS
================================================

[NEW] Found new migration: 004_create_user_table.sql
[INFO] Found 1 new migration(s) to apply.

Do you want to apply these migrations? (Y/N): Y

================================================
   APPLYING MIGRATIONS...
================================================

[RUNNING] Applying migration: 004_create_user_table.sql...
[OK] Successfully applied: 004_create_user_table.sql

================================================
   ALL MIGRATIONS APPLIED SUCCESSFULLY!
================================================
```

**Khi nào dùng:**
- Sau khi pull code mới từ Git
- Lần đầu setup project
- Khi có migration mới trong thư mục `migrations/`

---

### 2. `run_migration.bat` - Chạy 1 migration cụ thể

**Mô tả:**
- Chạy thủ công 1 file migration
- Dùng để test migration trước khi commit

**Cách dùng:**
```bash
cd database\scripts
run_migration.bat ..\migrations\004_create_user_table.sql
```

**Khi nào dùng:**
- Test migration mới vừa tạo
- Chạy lại migration bị lỗi sau khi sửa
- Debug migration

---

### 3. `scaffold_entities.bat` - Tạo C# entities từ database

**Mô tả:**
- Tự động tạo C# entity classes từ database schema
- Tạo `DbContext` để kết nối database
- Loại trừ bảng `__MigrationHistory`
- **Ghi đè** files cũ (nếu đã tồn tại)

**Cách dùng:**
```bash
cd database\scripts
scaffold_entities.bat
```

**Script sẽ làm gì:**
1. ✅ Kiểm tra Docker container
2. ✅ Chạy `dotnet ef dbcontext scaffold` cho các tables: `Roles`, `Users`, `UserRoles`
3. ✅ Tạo entity classes vào `BizFlow.Domain/Entities/`
4. ✅ Tạo `BizFlowDbContext.cs` vào `BizFlow.Infrastructure/Data/`
5. ✅ Tự động pluralize: Table `Roles` → Class `Role`

**Output mẫu:**
```
================================================
   SCAFFOLD ENTITIES FROM DATABASE
================================================

[STEP 1] Fetching latest database schema...

Build started...
Build succeeded.

================================================
   ENTITIES SCAFFOLDED SUCCESSFULLY!
================================================

[INFO] Generated files:
  - BizFlow.Domain/Entities/Role.cs
  - BizFlow.Domain/Entities/User.cs
  - BizFlow.Infrastructure/Data/BizFlowDbContext.cs
```

**⚠️ Lưu ý:**
- Files entity cũ sẽ bị **ghi đè hoàn toàn**
- Nếu đã custom entity (thêm methods, properties), hãy backup trước!
- Chỉ scaffold các tables đã list sẵn trong script

**Khi nào dùng:**
- Sau khi chạy migration tạo table mới
- Khi sửa schema database (thêm/xóa column)
- Sau khi pull code có migration mới

---

### 4. `scaffold_all.bat` - Scaffold TẤT CẢ tables

**Mô tả:**
- Tự động lấy danh sách TẤT CẢ tables từ database
- Scaffold tất cả (trừ `__MigrationHistory`)
- Hỏi xác nhận trước khi chạy

**Cách dùng:**
```bash
cd database\scripts
scaffold_all.bat
```

**Script sẽ làm gì:**
1. ✅ Query database để lấy tất cả tables: `SHOW TABLES;`
2. ✅ Lọc bỏ `__MigrationHistory`
3. ✅ Hiển thị danh sách tables sẽ scaffold
4. ✅ Hỏi xác nhận
5. ✅ Scaffold tất cả

**Output mẫu:**
```
================================================
   SCAFFOLD ALL ENTITIES (Exclude History)
================================================

[INFO] Getting list of tables from database...
[INFO] Will scaffold table: Roles
[INFO] Will scaffold table: Users
[INFO] Will scaffold table: UserRoles

Do you want to scaffold all these tables? (Y/N): Y

[RUNNING] Scaffolding all entities...
```

**Khi nào dùng:**
- Khi có nhiều tables mới
- Muốn refresh toàn bộ entities
- Lần đầu setup project (đã có sẵn database)

---

### 5. `scaffold_specific_tables.bat` - Scaffold tables chỉ định

**Mô tả:**
- Cho phép chọn tables cụ thể để scaffold
- Tránh ghi đè tất cả entities
- Interactive (hỏi input từ user)

**Cách dùng:**
```bash
cd database\scripts
scaffold_specific_tables.bat

# Script sẽ hỏi:
Table names: Roles Users
```

**Script sẽ làm gì:**
1. ✅ Hiển thị danh sách tables hiện có
2. ✅ Hỏi nhập tên tables (cách nhau bởi khoảng trắng)
3. ✅ Xác nhận danh sách
4. ✅ Scaffold chỉ các tables đã chọn

**Output mẫu:**
```
================================================
   SCAFFOLD SPECIFIC TABLES
================================================

[INFO] Available tables in database:

+----------------------+
| Tables_in_bizflow_db |
+----------------------+
| Roles                |
| Users                |
| UserRoles            |
| __MigrationHistory   |
+----------------------+

[INPUT] Enter table names separated by space
Example: Roles Users UserRoles

Table names: Roles Users

[INFO] Will scaffold these tables: Roles Users

Continue? (Y/N): Y

[RUNNING] Scaffolding entities...
```

**Input format:**
- Tên tables cách nhau bởi **khoảng trắng**
- **Phân biệt chữ hoa/thường** (case-sensitive)
- Ví dụ: `Roles Users UserRoles`

**Khi nào dùng:**
- Chỉ muốn scaffold 1-2 tables cụ thể
- Đã custom một số entities, chỉ muốn cập nhật vài cái
- Test scaffold trước khi chạy `scaffold_all.bat`

---

### 6. `view_database.bat` - Xem thông tin database

**Mô tả:**
- Hiển thị tổng quan về database
- Kiểm tra migrations đã chạy
- Đếm số lượng records

**Cách dùng:**
```bash
cd database\scripts
view_database.bat
```

**Script sẽ làm gì:**
1. ✅ Hiển thị tất cả tables: `SHOW TABLES;`
2. ✅ Hiển thị lịch sử migrations từ `__MigrationHistory`
3. ✅ Đếm số records trong bảng `Roles`

**Output mẫu:**
```
================================================
   DATABASE INFORMATION
================================================

[1] Show all tables:

+----------------------+
| Tables_in_bizflow_db |
+----------------------+
| Roles                |
| Users                |
| UserRoles            |
| __MigrationHistory   |
+----------------------+

[2] Show migration history:

+---------------------------+----------------+---------------------+
| MigrationId               | ProductVersion | AppliedAt           |
+---------------------------+----------------+---------------------+
| 001_create_initial_schema | 1.0.0          | 2026-01-25 16:21:47 |
| 002_add_role_table        | 1.0.0          | 2026-01-25 16:24:09 |
| 003_insert_default_roles  | 1.0.0          | 2026-01-25 16:24:29 |
+---------------------------+----------------+---------------------+

[3] Show table counts:

+-----------+----------+
| TableName | RowCount |
+-----------+----------+
| Roles     |        3 |
+-----------+----------+
```

**Khi nào dùng:**
- Kiểm tra database structure
- Xem migrations đã chạy chưa
- Debug khi có lỗi
- Kiểm tra data sau khi migration

---

## 📏 Quy tắc đặt tên file migration

**Format:** `{số}_{action}_{object}.sql`

**Ví dụ đúng:**
```
004_create_user_table.sql      ✅
005_add_email_to_users.sql     ✅
006_insert_default_users.sql   ✅
```

**Ví dụ sai:**
```
user_table.sql                 ❌ Thiếu số
4_create_user.sql             ❌ Số phải 3 chữ số (004)
004_users.sql                 ❌ Thiếu action
```

**Actions thường dùng:**
- `create` - Tạo table mới
- `add` - Thêm column
- `alter` - Sửa column/constraint
- `insert` - Thêm data
- `drop` - Xóa table/column
- `update` - Cập nhật data

---

## ⚠️ Lỗi thường gặp

### "MySQL container is not running"
```bash
# Khởi động Docker
docker-compose up -d
# Đợi 15 giây rồi chạy lại
```

### "Scaffold failed"
```bash
# Cài EF Core tools
dotnet tool install --global dotnet-ef

# Hoặc update nếu đã cài
dotnet tool update --global dotnet-ef
```

### "Duplicate entry for key PRIMARY"
```bash
# Migration đã chạy rồi nhưng thiếu `ON DUPLICATE KEY UPDATE`
# Sửa file migration, thêm:
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
```

### "Table doesn't exist" khi scaffold
```bash
# Chạy migrations trước
cd database\scripts
apply_new_migrations.bat

# Sau đó mới scaffold
scaffold_entities.bat
```

### Migration bị lỗi
```bash
# Xem logs chi tiết
docker-compose logs mysql

# Hoặc reset database (XÓA HẾT DỮ LIỆU!)
docker-compose down
docker volume rm bizflow-be-service_mysql_data
docker-compose up -d
```

---

## 💡 Lưu ý quan trọng

### 1. Migration phải Idempotent (chạy nhiều lần OK)

**Luôn dùng:**
- `CREATE TABLE IF NOT EXISTS`
- `ALTER TABLE ... IF NOT EXISTS` (MySQL 8.0+)
- `INSERT ... ON DUPLICATE KEY UPDATE`
- `INSERT IGNORE INTO`

### 2. Naming Convention

**Database:**
- Tables: Số nhiều (`Roles`, `Users`, `Products`)
- Columns: PascalCase (`CreatedAt`, `UpdatedAt`)

**C# Code:**
- Entity class: Số ít (`Role`, `User`, `Product`)
- Properties: PascalCase (`CreatedAt`, `UpdatedAt`)

### 3. Luôn track migration

Mọi migration phải có:
```sql
INSERT INTO __MigrationHistory (MigrationId, ProductVersion) 
VALUES ('004_create_user_table', '1.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = ProductVersion;
```

### 4. Test migration 2 lần

```bash
# Lần 1: Chạy migration mới
run_migration.bat ..\migrations\004_create_user_table.sql

# Lần 2: Chạy lại để test idempotent
run_migration.bat ..\migrations\004_create_user_table.sql
# Phải chạy OK, không lỗi!
```

### 5. Commit từng migration một

```bash
# Đúng ✅
git add database/migrations/004_create_user_table.sql
git commit -m "feat(db): add users table"

# Sai ❌ (gộp nhiều migrations)
git add database/migrations/*.sql
git commit -m "add all tables"
```

### 6. Backup trước khi scaffold

Nếu đã custom entity classes:
```bash
# Backup thư mục entities
xcopy BizFlow.Domain\Entities BizFlow.Domain\Entities.backup\ /E /I

# Sau đó mới scaffold
cd database\scripts
scaffold_entities.bat
```

---

## 🔄 Workflow team

**Developer A (tạo migration mới):**
```bash
# 1. Tạo migration
echo "CREATE TABLE ..." > database/migrations/004_create_user_table.sql

# 2. Test
cd database/scripts
run_migration.bat ..\migrations\004_create_user_table.sql
view_database.bat

# 3. Commit
git add database/migrations/004_create_user_table.sql
git commit -m "feat(db): add users table"
git push
```

**Developer B (pull code):**
```bash
# 1. Pull
git pull

# 2. Apply migrations
cd database/scripts
apply_new_migrations.bat

# 3. Scaffold entities
scaffold_entities.bat

# 4. Build
cd ..\..\bizflow-platform
dotnet build
```

---

**Cần trợ giúp?** Hỏi trong team chat hoặc lmthien.30@gmail.com
