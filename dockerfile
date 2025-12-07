# שלב 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# העתקת קבצי הפרויקט
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

# שלב 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# העתקת הפרויקט המפורסם
COPY --from=build /app/publish .

# יצירת תיקייה לשמירת מסד הנתונים (SQLite)
RUN mkdir -p /app/Data
ENV DataDirectory=/app/Data

# הפורט שהשרת משתמש בו
EXPOSE 5000

# הפעלת השרת
ENTRYPOINT ["dotnet", "KickRateServer.dll"]
