REM Quick helper for creating new migrations
echo %1
dotnet ef migrations add %1 -c ApplicationDbContext -o Database/Migrations

