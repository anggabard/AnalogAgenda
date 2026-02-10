# AnalogAgenda

## Deploy full stack to Docker (e.g. Ubuntu server)

The solution can be deployed as a full stack (SQL Server, Azurite, backend API, frontend, Azure Functions) using **Aspire** and Docker Compose. The AppHost is the single source of truth; Aspire generates the compose file and container images.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://aspire.dev/get-started/install-cli) (for `aspire` commands)
- Docker (on the machine where you run `aspire deploy` or `docker compose`)

### Generate and run on your machine

1. **Install Aspire CLI** (if not already):
   - Windows: `irm https://aspire.dev/install.ps1 | iex`
   - Linux/macOS: `curl -sSL https://aspire.dev/install.sh | bash`

2. **Set the SQL password** (used by the generated `.env`):
   - Create or edit user secrets for the AppHost:  
     `dotnet user-secrets set "Parameters:sql-password" "YourStrongPassword" --project AnalogAgenda.AppHost`
   - Or set the parameter when prompted during publish/deploy.

3. **Generate Docker Compose and run**:
   - From the repo root:
     - `aspire deploy` — builds images, generates `aspire-output/`, and runs `docker compose up`, or
     - `aspire do prepare-compose --environment docker` — only generates `aspire-output/` (compose + `.env` + images) so you can copy it to another machine.

4. **Fill in `.env`** in `aspire-output/` (e.g. `sql-password`, and any optional secrets for Functions).

### Run on the Ubuntu server

1. Copy the `aspire-output/` folder (from your dev machine) to the server.
2. On the server: `cd aspire-output && docker compose up -d`.
3. Only **Docker** is required on the server; no .NET or Aspire CLI needed.

### Environment variables (Docker deploy)

- **Backend**: Set `ASPNETCORE_ENVIRONMENT=Docker` so database migrations run on startup (or rely on the generated compose if it sets this).
- **Storage (Azurite)**: The stack includes an `azurite` container. Backend and Functions need the storage connection string (e.g. `ConnectionStrings__analogagendastorage` or `STORAGE_CONNECTION_STRING`). When using the included Azurite container, use:
  `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;`
- **Functions**: Azure AD, Container Registry, and SMTP are optional for the Docker stack; the app starts without them. For full Functions features (email, registry cleanup), configure those sections in app settings or env.

### Frontend (Docker environment)

For the Angular app to call the backend in Docker, use the **docker** build configuration (relative `apiUrl` when behind a reverse proxy, or set `apiUrl` in `src/environments/environment.docker.ts` to the backend URL before building):

- Build with Docker env: `npm run build:docker` in `analogagenda.client` (or `ng build --configuration docker`).