FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ShippingService.Domain/ShippingService.Domain.csproj src/ShippingService.Domain/
COPY src/ShippingService.Application/ShippingService.Application.csproj src/ShippingService.Application/
COPY src/ShippingService.Infrastructure/ShippingService.Infrastructure.csproj src/ShippingService.Infrastructure/
COPY src/ShippingService.Api/ShippingService.Api.csproj src/ShippingService.Api/
RUN dotnet restore src/ShippingService.Api/ShippingService.Api.csproj

COPY . .
RUN dotnet publish src/ShippingService.Api/ShippingService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ShippingService.Api.dll"]
