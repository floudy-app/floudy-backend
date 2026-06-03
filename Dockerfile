FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Floudy_API/Floudy_API.csproj Floudy_API/
RUN dotnet restore Floudy_API/Floudy_API.csproj

COPY Floudy_API/ Floudy_API/
RUN dotnet publish Floudy_API/Floudy_API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "Floudy.API.dll"]
