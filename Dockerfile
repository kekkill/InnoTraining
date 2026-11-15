FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG service
WORKDIR /src
COPY . .
RUN dotnet restore src/InnoShop.${service}.Api/InnoShop.${service}.Api.csproj --locked-mode -m:1
RUN dotnet publish src/InnoShop.${service}.Api/InnoShop.${service}.Api.csproj -c Release --no-restore -m:1 -o /out /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG service
ENV app_dll=InnoShop.${service}.Api.dll
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$app_dll\""]
