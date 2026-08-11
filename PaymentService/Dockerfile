FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/PaymentService.Domain/PaymentService.Domain.csproj src/PaymentService.Domain/
COPY src/PaymentService.Application/PaymentService.Application.csproj src/PaymentService.Application/
COPY src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj src/PaymentService.Infrastructure/
COPY src/PaymentService.Api/PaymentService.Api.csproj src/PaymentService.Api/
RUN dotnet restore src/PaymentService.Api/PaymentService.Api.csproj

COPY . .
RUN dotnet publish src/PaymentService.Api/PaymentService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]
