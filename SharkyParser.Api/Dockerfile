# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["SharkyParser.Api/SharkyParser.Api.csproj", "SharkyParser.Api/"]
COPY ["SharkyParser.Core/SharkyParser.Core.csproj", "SharkyParser.Core/"]

# Restore dependencies
RUN dotnet restore "SharkyParser.Api/SharkyParser.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "SharkyParser.Api/SharkyParser.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 5000
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "SharkyParser.Api.dll"]
