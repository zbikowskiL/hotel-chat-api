# Etap 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiuj plik projektu i przywróć zależności
COPY HotelChatApi.csproj .
RUN dotnet restore

# Kopiuj resztę kodu i zbuduj
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Etap 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Kopiuj zbudowaną aplikację
COPY --from=build /app/publish .

# Port na którym działa API
EXPOSE 8080

# Uruchom aplikację
ENTRYPOINT ["dotnet", "HotelChatApi.dll"]
