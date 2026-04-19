FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/StockSense.API/StockSense.API.csproj backend/StockSense.API/
RUN dotnet restore backend/StockSense.API/StockSense.API.csproj
COPY . .
RUN dotnet publish backend/StockSense.API/StockSense.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StockSense.API.dll"]
