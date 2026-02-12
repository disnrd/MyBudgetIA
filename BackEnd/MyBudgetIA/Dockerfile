# syntax=docker/dockerfile:1

# ============================
# BUILD STAGE (non-distroless)
# ============================
FROM mcr.microsoft.com/dotnet/sdk:10.0.103 AS build
WORKDIR /src

COPY MyBudgetIA.Api/MyBudgetIA.Api.csproj MyBudgetIA.Api/
COPY MyBudgetIA.Application/MyBudgetIA.Application.csproj MyBudgetIA.Application/
COPY MyBudgetIA.Infrastructure/MyBudgetIA.Infrastructure.csproj MyBudgetIA.Infrastructure/
COPY MyBudgetIA.Domain/MyBudgetIA.Domain.csproj MyBudgetIA.Domain/
COPY Shared/Shared.csproj Shared/

RUN dotnet restore MyBudgetIA.Api/MyBudgetIA.Api.csproj

COPY . ./
RUN dotnet publish MyBudgetIA.Api/MyBudgetIA.Api.csproj -c Release -o /app/publish --no-restore

# Create non-root user (Alpine)
RUN adduser -D -h /home/appuser appuser

# ============================
# RUNTIME STAGE (DISTROLESS)
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0.103

WORKDIR /app

# Copy passwd so the user exists in runtime
COPY --from=build /etc/passwd /etc/passwd

USER appuser

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "MyBudgetIA.Api.dll"]
