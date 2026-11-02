FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

COPY . ./

RUN dotnet restore "src/GeminiCode.Thomas.Api/GeminiCode.Thomas.Api.csproj"
RUN dotnet publish "src/GeminiCode.Thomas.Api/GeminiCode.Thomas.Api.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80  
EXPOSE 80

WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "GeminiCode.Thomas.Api.dll"]