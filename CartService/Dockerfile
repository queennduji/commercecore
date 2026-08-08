FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/CartService.Domain/CartService.Domain.csproj src/CartService.Domain/
COPY src/CartService.Application/CartService.Application.csproj src/CartService.Application/
COPY src/CartService.Infrastructure/CartService.Infrastructure.csproj src/CartService.Infrastructure/
COPY src/CartService.Api/CartService.Api.csproj src/CartService.Api/
RUN dotnet restore src/CartService.Api/CartService.Api.csproj

COPY . .
RUN dotnet publish src/CartService.Api/CartService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CartService.Api.dll"]
