 # Sample contents of Dockerfile
 # Stage 1
 FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
 WORKDIR /src

 # caches restore result by copying csproj file separately
 COPY src/GraphWebhooks-Core/*.csproj .
 RUN dotnet restore

 # copies the rest of your code
 COPY . .
 RUN dotnet publish --output /app/ --configuration Release

 # Stage 2
 FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
 WORKDIR /app
 COPY --from=builder /app .
 ENTRYPOINT ["dotnet", "GraphWebhooks-Core.dll"]
 EXPOSE 5000
 EXPOSE 5001