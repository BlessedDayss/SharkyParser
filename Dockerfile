# --- Stage 1: Build Angular Frontend ---
FROM node:20-alpine AS web-build
WORKDIR /src/web
COPY SharkyParser.Web/package*.json ./
RUN npm install
COPY SharkyParser.Web/ .
RUN npm run build -- --configuration production

# --- Stage 2: Build .NET Backend ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY ["SharkyParser.Api/SharkyParser.Api.csproj", "SharkyParser.Api/"]
COPY ["SharkyParser.Core/SharkyParser.Core.csproj", "SharkyParser.Core/"]
RUN dotnet restore "SharkyParser.Api/SharkyParser.Api.csproj"
COPY . .
RUN dotnet publish "SharkyParser.Api/SharkyParser.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Stage 3: Final Image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy API binaries
COPY --from=api-build /app/publish .

# Copy Angular static files to wwwroot
# Note: Angular 19 'application' builder outputs to dist/project-name/browser
COPY --from=web-build /src/web/dist/sharky-parser.web/browser ./wwwroot

# Expose port 5000
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "SharkyParser.Api.dll"]