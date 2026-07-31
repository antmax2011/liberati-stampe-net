FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM node:20 AS frontend
WORKDIR /frontend
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ .
RUN npm run build -- --configuration production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LiberatiStampe.API.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/out

FROM base AS final
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
WORKDIR /app
COPY --from=build /app/out .
COPY --from=frontend /frontend/dist/liberati-stampe ./wwwroot
RUN ls -la ./wwwroot || echo "WWWROOT VUOTO!"
ENTRYPOINT ["dotnet", "LiberatiStampe.API.dll"]
