FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Fintacharts.AssetTracker/Fintacharts.AssetTracker.csproj", "Fintacharts.AssetTracker/"]
RUN dotnet restore "Fintacharts.AssetTracker/Fintacharts.AssetTracker.csproj"

COPY . .
WORKDIR "/src/Fintacharts.AssetTracker"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fintacharts.AssetTracker.dll"]