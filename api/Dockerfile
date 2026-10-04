# API DindinBuddies: build com o SDK, execução só com o runtime do ASP.NET.
# Contexto de build: a pasta api/.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para o cache reaproveitar os pacotes quando só o código mudar.
COPY src/DindinBuddies.Domain/DindinBuddies.Domain.csproj src/DindinBuddies.Domain/
COPY src/DindinBuddies.Application/DindinBuddies.Application.csproj src/DindinBuddies.Application/
COPY src/DindinBuddies.Infrastructure/DindinBuddies.Infrastructure.csproj src/DindinBuddies.Infrastructure/
COPY src/DindinBuddies.Api/DindinBuddies.Api.csproj src/DindinBuddies.Api/
RUN dotnet restore src/DindinBuddies.Api/DindinBuddies.Api.csproj

COPY src/ src/
RUN dotnet publish src/DindinBuddies.Api/DindinBuddies.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# A imagem do .NET já escuta na 8080 e traz o usuário sem privilégios "app".
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "DindinBuddies.Api.dll"]
