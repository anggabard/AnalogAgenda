# Use the official .NET 10 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["AnalogAgenda.Server/AnalogAgenda.Server.csproj", "AnalogAgenda.Server/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["Configuration/Configuration.csproj", "Configuration/"]
COPY ["AnalogAgenda.ServiceDefaults/AnalogAgenda.ServiceDefaults.csproj", "AnalogAgenda.ServiceDefaults/"]
COPY ["AnalogAgenda.EmailSender/AnalogAgenda.EmailSender.csproj", "AnalogAgenda.EmailSender/"]

# Restore dependencies
RUN dotnet restore "AnalogAgenda.Server/AnalogAgenda.Server.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/AnalogAgenda.Server"
RUN dotnet build "AnalogAgenda.Server.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "AnalogAgenda.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 10 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Expose port 8080 (Azure Container Apps default)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "AnalogAgenda.Server.dll"]
