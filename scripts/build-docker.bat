@echo off
REM Build script for local Docker development (Windows)

echo Building Docker images...

REM Build backend
echo Building backend image...
docker build -f src\Backend\Dockerfile -t pm-backend:dev src\Backend

REM Build frontend
echo Building frontend image...
docker build -f src\Frontend\Dockerfile -t pm-frontend:dev src\Frontend

echo.
echo Images built successfully:
echo   - pm-backend:dev
echo   - pm-frontend:dev
echo.
echo Run 'docker-compose up -d' to start the application.
