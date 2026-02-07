FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files
COPY OneManVanFSM.Shared/OneManVanFSM.Shared.csproj OneManVanFSM.Shared/
COPY OneManVanFSM.Web/OneManVanFSM.Web.csproj OneManVanFSM.Web/

# Restore
RUN dotnet restore OneManVanFSM.Web/OneManVanFSM.Web.csproj

# Copy everything else
COPY OneManVanFSM.Shared/ OneManVanFSM.Shared/
COPY OneManVanFSM.Web/ OneManVanFSM.Web/

# Build
RUN dotnet build OneManVanFSM.Web/OneManVanFSM.Web.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish OneManVanFSM.Web/OneManVanFSM.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OneManVanFSM.Web.dll"]
