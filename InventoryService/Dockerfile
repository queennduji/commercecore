FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/InventoryService.Domain/InventoryService.Domain.csproj src/InventoryService.Domain/
COPY src/InventoryService.Application/InventoryService.Application.csproj src/InventoryService.Application/
COPY src/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj src/InventoryService.Infrastructure/
COPY src/InventoryService.Api/InventoryService.Api.csproj src/InventoryService.Api/
RUN dotnet restore src/InventoryService.Api/InventoryService.Api.csproj

COPY . .
RUN dotnet publish src/InventoryService.Api/InventoryService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
