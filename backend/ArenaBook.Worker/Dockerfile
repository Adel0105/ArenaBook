# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src
COPY backend/ArenaBook.sln ./
COPY backend/ArenaBook.Domain/ArenaBook.Domain.csproj backend/ArenaBook.Domain/
COPY backend/ArenaBook.Application/ArenaBook.Application.csproj backend/ArenaBook.Application/
COPY backend/ArenaBook.Infrastructure/ArenaBook.Infrastructure.csproj backend/ArenaBook.Infrastructure/
COPY backend/ArenaBook.Worker/ArenaBook.Worker.csproj backend/ArenaBook.Worker/
RUN dotnet restore backend/ArenaBook.Worker/ArenaBook.Worker.csproj
COPY backend/ArenaBook.Domain/ backend/ArenaBook.Domain/
COPY backend/ArenaBook.Application/ backend/ArenaBook.Application/
COPY backend/ArenaBook.Infrastructure/ backend/ArenaBook.Infrastructure/
COPY backend/ArenaBook.Worker/ backend/ArenaBook.Worker/
RUN dotnet publish backend/ArenaBook.Worker/ArenaBook.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArenaBook.Worker.dll"]
