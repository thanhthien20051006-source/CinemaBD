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

RUN dotnet restore CinemaBD.Api/CinemaBD.Api.csproj

COPY CinemaBD.Domain/        CinemaBD.Domain/
COPY CinemaBD.Application/   CinemaBD.Application/
COPY CinemaBD.Infrastructure/ CinemaBD.Infrastructure/
COPY CinemaBD.Api/           CinemaBD.Api/

RUN dotnet publish CinemaBD.Api/CinemaBD.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

 # Cai libfaketime de dong bo ngay test voi du lieu demo
RUN apt-get update -qq && apt-get install -y --no-install-recommends libfaketime && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
 # Khong fake gio runtime: dung gio that cua container + convert sang Asia/Ho_Chi_Minh trong code

EXPOSE 8080
ENTRYPOINT ["dotnet", "CinemaBD.Api.dll"]



