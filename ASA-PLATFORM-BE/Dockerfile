# Sử dụng .NET SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution (bao gồm tất cả projects)
COPY . .

# Restore packages (đường dẫn tới solution đúng)
RUN dotnet restore ASA-PLATFORM-BE.sln

# Publish project chính
RUN dotnet publish ASA-PLATFORM-BE/ASA-PLATFORM-BE.csproj -c Release -o /app/out

# Sử dụng runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

# Thiết lập entry point
ENTRYPOINT ["dotnet", "ASA-PLATFORM-BE.dll"]
