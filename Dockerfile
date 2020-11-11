 # Sample contents of Dockerfile
 # Stage 1
 FROM mcr.microsoft.com/dotnet/core/sdk:5.0 AS builder
 WORKDIR /src

 # caches restore result by copying csproj file separately
 COPY src/GraphWebhooks-Core/*.csproj .
 RUN dotnet restore

 # copies the rest of your code
 COPY . .
 RUN dotnet publish --output /app/ --configuration Release

 # Stage 2
 FROM mcr.microsoft.com/dotnet/core/aspnet:5.0
 WORKDIR /app
 COPY --from=builder /app .
 ENTRYPOINT ["dotnet", "GraphWebhooks-Core.dll"]
 VOLUME [ "/app/certificates" ]
 EXPOSE 5000 \
        5001
 ENV ASPNETCORE_URLS=http://*:5000;https://*:5001 \
     AZUREAD__INSTANCE=https://login.microsoftonline.com/ \
     AZUREAD__TENANTID=common \
     AZUREAD__CALLBACKPATH=/signin-oidc \
     AZUREAD__CLIENTID=clientid \
     KEYVAULTSETTINGS__CLIENTID=clientid \
     KEYVAULTSETTINGS__CLIENTSECRET=clientsecret \
     KEYVAULTSETTINGS__CERTIFICATEURL=https://keyvaultname.vault.azure.net/secrets/certificateName