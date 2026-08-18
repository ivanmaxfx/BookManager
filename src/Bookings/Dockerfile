FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore     src/Bookings/Bookings.Presentation/Bookings.Presentation.csproj

RUN dotnet publish     src/Bookings/Bookings.Presentation/Bookings.Presentation.csproj     -c Release     -o /app/publish     --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Bookings.Presentation.dll"]
