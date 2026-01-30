@echo off
SETLOCAL EnableDelayedExpansion

echo ================================================
echo    AUTO APPLY NEW MIGRATIONS
echo ================================================
echo.

:: Lấy đường dẫn thư mục hiện tại
set SCRIPT_DIR=%~dp0
set MIGRATIONS_DIR=%SCRIPT_DIR%..\migrations

:: Kiểm tra Docker container
docker ps | findstr bizflow-mysql >nul
if errorlevel 1 (
    echo [ERROR] MySQL container is not running!
    echo Please start Docker: docker-compose up -d
    pause
    exit /b 1
)

:: Tạo temp file để lưu danh sách migrations đã chạy từ DB
set TEMP_FILE=%TEMP%\applied_migrations_temp.txt
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -N -e "SELECT MigrationId FROM __MigrationHistory" 2>nul > "%TEMP_FILE%"

:: Nếu bảng __MigrationHistory chưa tồn tại, tạo file rỗng
if errorlevel 1 (
    type nul > "%TEMP_FILE%"
)

:: Đếm số migrations sẽ chạy
set COUNT=0

:: Duyệt qua tất cả file .sql trong migrations (theo thứ tự alphabet)
for /f "delims=" %%f in ('dir /b /on "%MIGRATIONS_DIR%\*.sql"') do (
    set FILENAME=%%f
    set MIGRATION_ID=%%~nf
    
    :: Kiểm tra xem đã chạy chưa (so với database)
    findstr /C:"!MIGRATION_ID!" "%TEMP_FILE%" >nul
    if errorlevel 1 (
        echo [NEW] Found new migration: !FILENAME!
        set /a COUNT+=1
    )
)

:: Xóa temp file
del "%TEMP_FILE%" 2>nul

if %COUNT%==0 (
    echo.
    echo [OK] No new migrations to apply.
    echo All migrations are up to date!
    pause
    exit /b 0
)

echo.
echo [INFO] Found %COUNT% new migration(s) to apply.
echo.
set /p CONFIRM="Do you want to apply these migrations? (Y/N): "

if /i not "%CONFIRM%"=="Y" (
    echo.
    echo [CANCELLED] Migration cancelled by user.
    pause
    exit /b 0
)

echo.
echo ================================================
echo    APPLYING MIGRATIONS...
echo ================================================
echo.

:: Chạy từng migration mới (theo thứ tự)
for /f "delims=" %%f in ('dir /b /on "%MIGRATIONS_DIR%\*.sql"') do (
    set FILENAME=%%f
    set MIGRATION_ID=%%~nf
    
    :: Kiểm tra lại xem đã chạy chưa
    docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -N -e "SELECT COUNT(*) FROM __MigrationHistory WHERE MigrationId='!MIGRATION_ID!'" 2>nul | findstr "1" >nul
    if errorlevel 1 (
        echo [RUNNING] Applying migration: !FILENAME!...
        
        :: Chạy migration và lưu exit code
        docker exec -i bizflow-mysql mysql -uadmin -padmin bizflow_db < "%MIGRATIONS_DIR%\!FILENAME!" 2>&1
        set MIGRATION_EXIT_CODE=!ERRORLEVEL!
        
        if !MIGRATION_EXIT_CODE! neq 0 (
            echo [ERROR] Failed to apply migration: !FILENAME!
            echo Please check the error above and fix it.
            pause
            exit /b 1
        )
        
        echo [OK] Successfully applied: !FILENAME!
        echo.
    )
)

echo.
echo ================================================
echo    ALL MIGRATIONS APPLIED SUCCESSFULLY!
echo ================================================
echo.
pause