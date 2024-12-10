#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /ProductionCoreApp
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["/ProductionCoreApp/ProductionCoreApp.csproj", "."]
RUN dotnet restore "./././ProductionCoreApp.csproj"
COPY ./ProductionCoreApp/.
WORKDIR "/src/."
RUN dotnet build "./ProductionCoreApp.csproj" -c $BUILD_CONFIGURATION -o /ProductionCoreApp/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProductionCoreApp.csproj" -c $BUILD_CONFIGURATION -o /ProductionCoreApp/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /ProductionCoreApp
COPY --from=publish /ProductionCoreApp/publish .
ENTRYPOINT ["dotnet", "/ProductionCoreApp/ProductionCoreApp.dll"]
