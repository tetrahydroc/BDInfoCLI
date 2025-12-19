# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .

RUN dotnet restore BDInfo/BDInfo.csproj
RUN dotnet publish BDInfo/BDInfo.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["./BDInfo"]
