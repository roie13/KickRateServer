using Microsoft.EntityFrameworkCore;
using KickRateServer.Data;
using KickRateServer.Models;
using KickRateServer.DTOs;

var builder = WebApplication.CreateBuilder(args);

// ✅ הגדרת האזנה על כל ה-IPs
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

// שימוש ב-SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=kickrate.db"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// הרשמה
app.MapPost("/auth/register", async (RegisterDto dto, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
    {
        return Results.BadRequest(new { message = "שם המשתמש כבר קיים" });
    }

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    // ✅ אם המשתמש הוא "roie", הגדר אותו כ-Admin
    var isAdmin = dto.Username.ToLower() == "roie";

    var user = new User
    {
        Username = dto.Username,
        PasswordHash = passwordHash,
        IsAdmin = isAdmin,
        CreatedAt = DateTime.Now
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        isAdmin = user.IsAdmin,
        message = "ההרשמה בוצעה בהצלחה"
    });
});

// התחברות
app.MapPost("/auth/login", async (LoginDto dto, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

    if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
    {
        return Results.BadRequest(new { message = "שם משתמש או סיסמה שגויים" });
    }

    // ✅ החזר גם את הסטטוס Admin
    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        isAdmin = user.IsAdmin,
        message = "התחברת בהצלחה"
    });
});

// כל המשתמשים
app.MapGet("/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Select(u => new { u.Id, u.Username, u.IsAdmin, u.CreatedAt })
        .ToListAsync();
    return Results.Ok(users);
});

// ✅ המשחק הבא - עם תוצאות
app.MapGet("/games/next", async (AppDbContext db) =>
{
    var nextGame = await db.Games
        .Where(g => g.GameDate >= DateTime.Now)
        .OrderBy(g => g.GameDate)
        .FirstOrDefaultAsync();

    if (nextGame == null)
    {
        return Results.NotFound(new { message = "אין משחקים קרובים" });
    }

    return Results.Ok(new
    {
        id = nextGame.Id,
        gameDate = nextGame.GameDate.ToString("yyyy-MM-dd"),
        gameTime = nextGame.GameDate.ToString("HH:mm"),
        location = nextGame.Location,
        opponent = nextGame.Opponent,
        goalsFor = nextGame.GoalsFor,
        goalsAgainst = nextGame.GoalsAgainst,
        result = nextGame.Result,
        createdByUserId = nextGame.CreatedByUserId
    });
});

// ✅ כל המשחקים - עם תוצאות
app.MapGet("/games", async (AppDbContext db) =>
{
    var games = await db.Games
        .OrderBy(g => g.GameDate)
        .Select(g => new
        {
            id = g.Id,
            gameDate = g.GameDate.ToString("yyyy-MM-dd"),
            gameTime = g.GameDate.ToString("HH:mm"),
            location = g.Location,
            opponent = g.Opponent,
            goalsFor = g.GoalsFor,
            goalsAgainst = g.GoalsAgainst,
            result = g.Result,
            createdByUserId = g.CreatedByUserId,
            createdAt = g.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(games);
});

// ✅ משחקים שעברו - עם תוצאות
app.MapGet("/games/past", async (AppDbContext db) =>
{
    var pastGames = await db.Games
        .Where(g => g.GameDate < DateTime.Now)
        .OrderByDescending(g => g.GameDate)
        .Select(g => new
        {
            id = g.Id,
            gameDate = g.GameDate.ToString("yyyy-MM-dd"),
            gameTime = g.GameDate.ToString("HH:mm"),
            location = g.Location,
            opponent = g.Opponent,
            goalsFor = g.GoalsFor,
            goalsAgainst = g.GoalsAgainst,
            result = g.Result,
            createdByUserId = g.CreatedByUserId
        })
        .ToListAsync();

    return Results.Ok(pastGames);
});

// יצירת משחק
app.MapPost("/games", async (CreateGameDto dto, AppDbContext db) =>
{
    try
    {
        // שילוב התאריך והשעה ל-DateTime אחד
        var gameDateTime = DateTime.Parse($"{dto.GameDate} {dto.GameTime}");

        var game = new Game
        {
            GameDate = gameDateTime,
            Location = dto.Location,
            Opponent = dto.Opponent,
            CreatedByUserId = dto.CreatedByUserId,
            CreatedAt = DateTime.Now
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();

        return Results.Created($"/games/{game.Id}", new
        {
            id = game.Id,
            gameDate = game.GameDate.ToString("yyyy-MM-dd"),
            gameTime = game.GameDate.ToString("HH:mm"),
            location = game.Location,
            opponent = game.Opponent,
            createdByUserId = game.CreatedByUserId
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = $"שגיאה ביצירת משחק: {ex.Message}" });
    }
});

// ✅ עדכון תוצאות משחק (Admin בלבד)
app.MapPut("/games/{gameId}/result", async (int gameId, UpdateGameResultDto dto, AppDbContext db) =>
{
    var game = await db.Games.FindAsync(gameId);

    if (game == null)
    {
        return Results.NotFound(new { message = "משחק לא נמצא" });
    }

    // עדכן תוצאות
    game.GoalsFor = dto.GoalsFor;
    game.GoalsAgainst = dto.GoalsAgainst;

    // חשב תוצאה
    if (dto.GoalsFor > dto.GoalsAgainst)
    {
        game.Result = "win";
    }
    else if (dto.GoalsFor < dto.GoalsAgainst)
    {
        game.Result = "loss";
    }
    else
    {
        game.Result = "draw";
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = game.Id,
        goalsFor = game.GoalsFor,
        goalsAgainst = game.GoalsAgainst,
        result = game.Result,
        message = "התוצאה עודכנה בהצלחה"
    });
});

// ✅ קבלת כל השחקנים עם ממוצע דירוג
app.MapGet("/players", async (AppDbContext db) =>
{
    var players = await db.Users
        .Select(u => new
        {
            id = u.Id,
            username = u.Username,
            averageRating = db.Ratings
                .Where(r => r.RatedUserId == u.Id)
                .Average(r => (double?)r.Stars) ?? 0.0,
            totalRatings = db.Ratings
                .Count(r => r.RatedUserId == u.Id)
        })
        .OrderByDescending(u => u.averageRating)
        .ToListAsync();

    return Results.Ok(players);
});

// ✅ שמירת או עדכון דירוג
app.MapPost("/ratings", async (RatingDto dto, AppDbContext db) =>
{
    // בדיקה שלא מדרגים את עצמך
    if (dto.RaterUserId == dto.RatedUserId)
    {
        return Results.BadRequest(new { message = "לא ניתן לדרג את עצמך" });
    }

    // בדיקה שהמשתמשים קיימים
    var raterExists = await db.Users.AnyAsync(u => u.Id == dto.RaterUserId);
    var ratedExists = await db.Users.AnyAsync(u => u.Id == dto.RatedUserId);

    if (!raterExists || !ratedExists)
    {
        return Results.BadRequest(new { message = "משתמש לא קיים" });
    }

    // בדיקה אם כבר יש דירוג
    var existingRating = await db.Ratings
        .FirstOrDefaultAsync(r => r.RaterUserId == dto.RaterUserId && r.RatedUserId == dto.RatedUserId);

    if (existingRating != null)
    {
        // עדכן דירוג קיים
        existingRating.Stars = dto.Stars;
        existingRating.UpdatedAt = DateTime.Now;
    }
    else
    {
        // צור דירוג חדש
        var rating = new Rating
        {
            RaterUserId = dto.RaterUserId,
            RatedUserId = dto.RatedUserId,
            Stars = dto.Stars,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        db.Ratings.Add(rating);
    }

    await db.SaveChangesAsync();

    return Results.Ok(new { message = "הדירוג נשמר בהצלחה" });
});

// ✅ קבלת הדירוג שנתתי למשתמש מסוים
app.MapGet("/ratings/{raterUserId}/{ratedUserId}", async (int raterUserId, int ratedUserId, AppDbContext db) =>
{
    var rating = await db.Ratings
        .FirstOrDefaultAsync(r => r.RaterUserId == raterUserId && r.RatedUserId == ratedUserId);

    if (rating == null)
    {
        return Results.Ok(new { stars = 0 });
    }

    return Results.Ok(new { stars = rating.Stars });
});

Console.WriteLine("🚀 Server starting on port 8080...");
app.Run();

// DTOs
public record LoginDto(string Username, string Password);
public record RegisterDto(string Username, string Password);
public record CreateGameDto(string GameDate, string GameTime, string Location, string Opponent, int CreatedByUserId);
public record RatingDto(int RaterUserId, int RatedUserId, int Stars);
public record UpdateGameResultDto(int GoalsFor, int GoalsAgainst);