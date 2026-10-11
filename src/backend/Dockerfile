# Build context is the repo root (render.yaml: dockerContext: .); the build needs the root
# build files (global.json, Directory.*.props, nuget.config, stylecop.json).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props nuget.config stylecop.json .editorconfig ./
COPY src/backend ./src/backend
RUN dotnet publish src/backend/SlotBook.Api/SlotBook.Api.csproj -c Release -o /app --no-self-contained

# Chiseled "extra": non-root, no shell, ships ICU and tzdata (IANA time zones).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SlotBook.Api.dll"]
