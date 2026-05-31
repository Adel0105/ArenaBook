# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src
COPY backend/ArenaBook.sln ./
COPY backend/ArenaBook.Domain/ArenaBook.Domain.csproj backend/ArenaBook.Domain/
COPY backend/ArenaBook.Application/ArenaBook.Application.csproj backend/ArenaBook.Application/
COPY backend/ArenaBook.Infrastructure/ArenaBook.Infrastructure.csproj backend/ArenaBook.Infrastructure/
COPY backend/ArenaBook.Api/ArenaBook.Api.csproj backend/ArenaBook.Api/
RUN dotnet restore backend/ArenaBook.Api/ArenaBook.Api.csproj
COPY backend/ArenaBook.Domain/ backend/ArenaBook.Domain/
COPY backend/ArenaBook.Application/ backend/ArenaBook.Application/
COPY backend/ArenaBook.Infrastructure/ backend/ArenaBook.Infrastructure/
COPY backend/ArenaBook.Api/ backend/ArenaBook.Api/
RUN dotnet publish backend/ArenaBook.Api/ArenaBook.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ArenaBook.Api.dll"]
