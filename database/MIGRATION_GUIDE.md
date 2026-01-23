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
-- Date: 2026-01-23
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
ON DUPLICATE KEY UPDATE MigrationId = MigrationId;
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

## � Quy tắc đặt tên file migration

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
- `alter` - Sửa column
- `insert` - Thêm data
- `drop` - Xóa table/column

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
```

### Migration bị lỗi
```bash
# Xem logs
docker-compose logs mysql

# Reset database (XÓA HẾT DỮ LIỆU!)
docker-compose down
docker volume rm bizflow-be-service_mysql_data
docker-compose up -d
```

---

## 💡 Lưu ý quan trọng

1. **Luôn dùng `IF NOT EXISTS`** khi CREATE TABLE
2. **Luôn track migration** vào `__MigrationHistory`
3. **Test migration 2 lần** để đảm bảo không lỗi khi chạy lại
4. **Commit từng migration một**, đừng gộp nhiều thay đổi

---

**Cần trợ giúp?** Hỏi trong team chat hoặc lmthien.30@gmail.com
