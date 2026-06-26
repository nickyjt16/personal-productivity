@echo off
REM Launches Productivity Hub: starts the API (which serves the built SPA) and
REM opens the dashboard in the default browser.
setlocal
cd /d "%~dp0"

REM Open the browser shortly after launch (server needs a moment to bind).
start "" /b cmd /c "timeout /t 3 /nobreak >nul & start http://localhost:5180"

echo Starting Productivity Hub on http://localhost:5180
echo Close this window to stop the app.
dotnet run --project "src\ProductivityHub.Api\ProductivityHub.Api.csproj" -c Release

endlocal
