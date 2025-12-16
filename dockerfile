# שלב 1 – Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# העתקה של קבצי הפרויקט
COPY *.csproj ./
RUN dotnet restore

# העתקת כל שאר הקבצים
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# שלב 2 – Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# העתקת קבצי ה־publish מהשלב הראשון
COPY --from=build /app/publish .

# הגדרת פורט (ASP.NET 9 כבר מקשיב ל־8080 כברירת מחדל בתוך Docker)
EXPOSE 8080

# הפעלת השרת
ENTRYPOINT ["dotnet", "KickRateServer.dll"]
