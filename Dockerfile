# Image de production - construite par la CI (.github/workflows/deploiement.yml) et publiee
# sur ghcr.io/thelpi/kikole, jamais sur le serveur, qui n'a pas de SDK .NET.

# --- build -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# le .csproj seul d'abord : tant qu'il ne change pas, la couche de restauration est
# reutilisee et le build ne retelecharge rien
COPY KikoleSite/KikoleSite.csproj KikoleSite/
RUN dotnet restore KikoleSite/KikoleSite.csproj
COPY KikoleSite/ KikoleSite/
RUN dotnet publish KikoleSite/KikoleSite.csproj -c Release -o /app/publish --no-restore

# --- execution -------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# 8080 et non 80 : le port par defaut des images .NET depuis la 8, et celui que le
# serveur attend
ENV ASPNETCORE_HTTP_PORTS=8080
# cles Data Protection (cf. Program.cs) : ce repertoire est un volume sur le serveur, sans
# quoi chaque deploiement deconnecterait tout le monde
ENV DataProtection__KeysPath=/app/keys
COPY --from=build /app/publish .
# les deux repertoires montes depuis le serveur doivent etre accessibles en ecriture a
# l'utilisateur du conteneur : les cles tournent seules, et les medias d'indice sont
# ecrits par l'admin (AdminController.UploadClueMedia)
RUN mkdir -p /app/keys /app/wwwroot/media && chown app:app /app/keys /app/wwwroot/media
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "KikoleSite.dll"]
