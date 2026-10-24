FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/DossierClinic.Shared/ DossierClinic.Shared/
COPY src/DossierClinic.Api/ DossierClinic.Api/
RUN dotnet publish DossierClinic.Api -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080 ApiKey=change-me-in-production
VOLUME /app/App_Data
EXPOSE 8080
ENTRYPOINT ["dotnet", "DossierClinic.Api.dll"]
