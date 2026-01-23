@echo off
echo ================================================
echo    DATABASE INFORMATION
echo ================================================
echo.

:: Kiểm tra Docker container
docker ps | findstr bizflow-mysql >nul
if errorlevel 1 (
    echo [ERROR] MySQL container is not running!
    pause
    exit /b 1
)

echo [1] Show all tables:
echo.
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -e "SHOW TABLES;" -t

echo.
echo [2] Show migration history:
echo.
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -e "SELECT * FROM __MigrationHistory;" -t

echo.
echo [3] Show table counts:
echo.
docker exec bizflow-mysql mysql -uadmin -padmin bizflow_db -e "SELECT 'Roles' as TableName, COUNT(*) as RowCount FROM Roles;" -t

echo.
pause