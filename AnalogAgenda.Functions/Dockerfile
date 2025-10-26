# Use the official Azure Functions base image for .NET 8 isolated
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

# Set working directory
WORKDIR /home/site/wwwroot

# Copy the Functions project files
COPY ["AnalogAgenda.Functions/AnalogAgenda.Functions.csproj", "AnalogAgenda.Functions/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["Configuration/Configuration.csproj", "Configuration/"]
COPY ["AnalogAgenda.EmailSender/AnalogAgenda.EmailSender.csproj", "AnalogAgenda.EmailSender/"]

# Restore dependencies
RUN dotnet restore "AnalogAgenda.Functions/AnalogAgenda.Functions.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the Functions app
WORKDIR "/home/site/wwwroot/AnalogAgenda.Functions"
RUN dotnet publish "AnalogAgenda.Functions.csproj" -c Release -o /home/site/wwwroot

# Set environment variables
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureWebJobsStorage=""
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
ENV ASPNETCORE_URLS=http://+:8080

# Expose port 8080 (Azure Container Apps default)
EXPOSE 8080
