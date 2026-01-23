@echo off
SETLOCAL EnableDelayedExpansion

echo ================================================
echo    AUTO APPLY NEW MIGRATIONS
echo ================================================
echo.

:: Lấy đường dẫn thư mục hiện tại
set SCRIPT_DIR=%~dp0
set MIGRATIONS_DIR=%SCRIPT_DIR%..\migrations
set APPLIED_FILE=%SCRIPT_DIR%..\applied_migrations.txt

:: Tạo file track nếu chưa có
if not exist "%APPLIED_FILE%" (
    echo Creating applied_migrations.txt...
    type nul > "%APPLIED_FILE%"
)

:: Đếm số migrations sẽ chạy
set COUNT=0

:: Duyệt qua tất cả file .sql trong migrations
for %%f in ("%MIGRATIONS_DIR%\*.sql") do (
    set FILENAME=%%~nxf
    
    :: Kiểm tra xem đã chạy chưa
    findstr /C:"!FILENAME!" "%APPLIED_FILE%" >nul
    if errorlevel 1 (
        echo [NEW] Found new migration: !FILENAME!
        set /a COUNT+=1
    )
)

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

:: Chạy từng migration mới
for %%f in ("%MIGRATIONS_DIR%\*.sql") do (
    set FILENAME=%%~nxf
    set FILEPATH=%%f
    
    :: Kiểm tra xem đã chạy chưa
    findstr /C:"!FILENAME!" "%APPLIED_FILE%" >nul
    if errorlevel 1 (
        echo [RUNNING] Applying migration: !FILENAME!...
        
        :: Chạy migration
        docker exec -i bizflow-mysql mysql -uadmin -padmin bizflow_db < "!FILEPATH!"
        
        if errorlevel 1 (
            echo [ERROR] Failed to apply migration: !FILENAME!
            echo Please check the error above and fix it.
            pause
            exit /b 1
        )
        
        :: Lưu vào file đã chạy
        echo !FILENAME! >> "%APPLIED_FILE%"
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