# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CinemaBD.WebApi.sln ./
COPY CinemaBD.Domain/CinemaBD.Domain.csproj         CinemaBD.Domain/
COPY CinemaBD.Application/CinemaBD.Application.csproj CinemaBD.Application/
COPY CinemaBD.Infrastructure/CinemaBD.Infrastructure.csproj CinemaBD.Infrastructure/
COPY CinemaBD.Api/CinemaBD.Api.csproj               CinemaBD.Api/
COPY CinemaBD.Web/CinemaBD.Web.csproj               CinemaBD.Web/

COPY CinemaBD.sql ./CinemaBD.sql

RUN dotnet restore CinemaBD.Web/CinemaBD.Web.csproj

COPY CinemaBD.Domain/        CinemaBD.Domain/
COPY CinemaBD.Application/   CinemaBD.Application/
COPY CinemaBD.Infrastructure/ CinemaBD.Infrastructure/
COPY CinemaBD.Web/           CinemaBD.Web/

RUN dotnet publish CinemaBD.Web/CinemaBD.Web.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8090
 # Gia lap ngay runtime ve 2026-05-17 de phim/suat chieu demo hien du lieu

EXPOSE 8090
ENTRYPOINT ["dotnet", "CinemaBD.Web.dll"]



