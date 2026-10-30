# Сборка: docker build -t freshday-leadbot server/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/FreshDay.LeadBot/FreshDay.LeadBot.csproj src/FreshDay.LeadBot/
RUN dotnet restore src/FreshDay.LeadBot/FreshDay.LeadBot.csproj
COPY src/ src/
RUN dotnet publish src/FreshDay.LeadBot/FreshDay.LeadBot.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Папка для архива заявок (data/leads.jsonl) — подключается как том, чтобы переживать обновления.
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
VOLUME /app/data
ENTRYPOINT ["dotnet", "FreshDay.LeadBot.dll"]
