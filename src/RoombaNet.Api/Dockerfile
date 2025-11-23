# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0.100-alpine3.22 AS build
WORKDIR /src

# Copy solution and project files
COPY global.json .
COPY RoombaNet.slnx .
COPY src/RoombaNet.Api/RoombaNet.Api.csproj src/RoombaNet.Api/
COPY src/RoombaNet.Core/RoombaNet.Core.csproj src/RoombaNet.Core/
COPY src/RoombaNet.Settings/RoombaNet.Settings.csproj src/RoombaNet.Settings/
COPY src/RoombaNet.Transport.Mqtt/RoombaNet.Transport.Mqtt.csproj src/RoombaNet.Transport.Mqtt/
COPY src/RoombaNet.Transport.Tls/RoombaNet.Transport.Tls.csproj src/RoombaNet.Transport.Tls/

# Restore dependencies
RUN dotnet restore src/RoombaNet.Api/RoombaNet.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/RoombaNet.Api
RUN dotnet publish -c Release -o /app/publish \
    --no-restore \
    --self-contained false \
    /p:GenerateDocumentationFile=false \
    /p:ExcludeFilesFromPublish="**/RoombaNet.Settings/appsettings*.json"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-alpine3.22 AS final
WORKDIR /app

# Create non-root user
RUN addgroup -g 1000 roomba && \
    adduser -u 1000 -G roomba -s /bin/sh -D roomba

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R roomba:roomba /app

# Switch to non-root user
USER roomba

# Expose port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/roomba/health || exit 1

ENTRYPOINT ["dotnet", "RoombaNet.Api.dll"]
