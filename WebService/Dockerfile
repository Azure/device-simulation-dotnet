FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 9002

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY ["device-simulation/device-simulation.sln", "device-simulation/"]
COPY ["device-simulation/Services/Services.csproj", "device-simulation/Services/"]
COPY ["device-simulation/SimulationAgent/SimulationAgent.csproj", "device-simulation/SimulationAgent/"]
COPY ["device-simulation/WebService/WebService.csproj", "device-simulation/WebService/"]
COPY ["device-simulation/WebService/azds_entrypoint.sh", "device-simulation/WebService/azds_entrypoint.sh"]

RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR "/src/device-simulation/WebService"
RUN dotnet build "WebService.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "WebService.csproj" -c Release -o /app

FROM base AS final
COPY --from=publish /app .
ENTRYPOINT  ["sh", "/src/device-simulation/WebService/azds_entrypoint.sh"]