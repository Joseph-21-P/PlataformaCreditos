# Usa la imagen del SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia los archivos del proyecto y restaura dependencias
COPY ["PlataformaCreditosWeb/PlataformaCreditosWeb.csproj", "PlataformaCreditosWeb/"]
RUN dotnet restore "PlataformaCreditosWeb/PlataformaCreditosWeb.csproj"

# Copia el resto del código y compila
COPY . .
WORKDIR "/src/PlataformaCreditosWeb"
RUN dotnet publish "PlataformaCreditosWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Usa la imagen de ASP.NET para ejecutar (más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expone el puerto que Render necesita
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PlataformaCreditosWeb.dll"]