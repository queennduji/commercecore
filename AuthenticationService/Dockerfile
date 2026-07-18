FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/AuthenticationService.Domain/AuthenticationService.Domain.csproj src/AuthenticationService.Domain/
COPY src/AuthenticationService.Application/AuthenticationService.Application.csproj src/AuthenticationService.Application/
COPY src/AuthenticationService.Infrastructure/AuthenticationService.Infrastructure.csproj src/AuthenticationService.Infrastructure/
COPY src/AuthenticationService.Api/AuthenticationService.Api.csproj src/AuthenticationService.Api/
RUN dotnet restore src/AuthenticationService.Api/AuthenticationService.Api.csproj

COPY . .
RUN dotnet publish src/AuthenticationService.Api/AuthenticationService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthenticationService.Api.dll"]
