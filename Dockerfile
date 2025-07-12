FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MarketPriceApi/MarketPriceApi.csproj", "MarketPriceApi/"]
RUN dotnet restore "MarketPriceApi/MarketPriceApi.csproj"
COPY . .
WORKDIR "/src/MarketPriceApi"
RUN dotnet build "MarketPriceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketPriceApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketPriceApi.dll"] 