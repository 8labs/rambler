export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000

nohup dotnet Rambler.Server/bin/server/Rambler.Server.dll &> rambler.log&