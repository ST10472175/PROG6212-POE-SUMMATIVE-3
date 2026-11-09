FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT=RaceDay.Mvc
WORKDIR /src
COPY . .
RUN dotnet restore "${PROJECT}/${PROJECT}.csproj"
RUN dotnet publish "${PROJECT}/${PROJECT}.csproj" --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
ARG PROJECT=RaceDay.Mvc
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV APP_DLL=${PROJECT}.dll
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet $APP_DLL"]
