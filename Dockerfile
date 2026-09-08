FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LeadGym.AI.csproj", "."]
RUN dotnet restore "LeadGym.AI.csproj"

COPY . .
RUN dotnet publish "LeadGym.AI.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LeadGym.AI.dll"]