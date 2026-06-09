# Stage 1: Compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copiar solución y csproj para restaurar las dependencias
COPY *.sln ./
COPY ToDoList_API/ToDoList_API.csproj ./ToDoList_API/
COPY ToDoList.Core/ToDoList.Core.csproj ./ToDoList.Core/
COPY ToDoList.Infrastructure/ToDoList.Infrastructure.csproj ./ToDoList.Infrastructure/
COPY ToDoList.Test/ToDoList.Test.csproj ./ToDoList.Test/

# Restaurar paquetes NuGet
RUN dotnet restore

# Copiar todo el código fuente restante
COPY . ./

# Publicar la aplicación en modo Release
RUN dotnet publish ToDoList_API/ToDoList_API.csproj -c Release -o out

# Stage 2: Imagen de ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# Configurar variables de entorno indispensables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Exponer el puerto del contenedor
EXPOSE 8080

# Iniciar la API
ENTRYPOINT ["dotnet", "ToDoList_API.dll"]
