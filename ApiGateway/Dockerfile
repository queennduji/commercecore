FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ApiGateway.Api/ApiGateway.Api.csproj src/ApiGateway.Api/
RUN dotnet restore src/ApiGateway.Api/ApiGateway.Api.csproj

COPY . .
RUN dotnet publish src/ApiGateway.Api/ApiGateway.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiGateway.Api.dll"]
