FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y expect
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Cambiar de nuevo al usuario de la app si lo necesitas
# USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["local-gpss.csproj", "./"]
RUN dotnet restore "local-gpss.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "local-gpss.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "local-gpss.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY startup.expect .
RUN chmod +x startup.expect

ENTRYPOINT ["./startup.expect"]
