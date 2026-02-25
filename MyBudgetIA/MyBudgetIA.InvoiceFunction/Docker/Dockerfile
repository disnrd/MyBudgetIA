FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

# === BUILD STAGE ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files only
COPY MyBudgetIA.InvoiceFunction/MyBudgetIA.InvoiceFunction.csproj MyBudgetIA.InvoiceFunction/
COPY Shared/Shared.csproj Shared/
COPY MyBudgetIA.Application/MyBudgetIA.Application.csproj MyBudgetIA.Application/
COPY MyBudgetIA.Domain/MyBudgetIA.Domain.csproj MyBudgetIA.Domain/
COPY MyBudgetIA.Infrastructure/MyBudgetIA.Infrastructure.csproj MyBudgetIA.Infrastructure/

RUN dotnet restore MyBudgetIA.InvoiceFunction/MyBudgetIA.InvoiceFunction.csproj

# Copy the rest of the source
COPY MyBudgetIA.InvoiceFunction/ MyBudgetIA.InvoiceFunction/
COPY Shared/ Shared/
COPY MyBudgetIA.Application/ MyBudgetIA.Application/
COPY MyBudgetIA.Domain/ MyBudgetIA.Domain/
COPY MyBudgetIA.Infrastructure/ MyBudgetIA.Infrastructure/

WORKDIR /src/MyBudgetIA.InvoiceFunction
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================ # FINAL STAGE # ============================ 
FROM base AS final 
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTION_APP_NAME=invoice-function \
    TZ=Europe/Luxembourg \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false