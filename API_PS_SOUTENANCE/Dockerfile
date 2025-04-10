# Étape 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copier et restaurer les dépendances
COPY *.csproj ./
RUN dotnet restore

# Copier le reste du code source et compiler l'application
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Étape 2 : Image d'exécution
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copier l'application compilée
COPY --from=build-env /app/out .

# Exposer le port (aligné avec docker-compose)
EXPOSE 3000

# Lancer l'application
ENTRYPOINT ["dotnet", "API_PS_SOUTENANCE.dll"]