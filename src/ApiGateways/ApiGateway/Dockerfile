FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ApiGateways/ApiGateway/ApiGateway.csproj", "src/ApiGateways/ApiGateway/"]
RUN dotnet restore "src/ApiGateways/ApiGateway/ApiGateway.csproj"
COPY . .
WORKDIR "/src/src/ApiGateways/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
