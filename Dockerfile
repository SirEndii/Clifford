FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Clifford/Clifford.csproj", "Clifford/"]
RUN dotnet restore "Clifford/Clifford.csproj"
COPY . .
WORKDIR "/src/Clifford"
RUN dotnet build "Clifford.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Clifford.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Clifford.dll"]
