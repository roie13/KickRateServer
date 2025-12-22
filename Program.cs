using Microsoft.EntityFrameworkCore;
using KickRateServer.Data;
using KickRateServer.Models;
using KickRateServer.DTOs;

var builder = WebApplication.CreateBuilder(args);

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

// ✅ המשחק הבא - ללא circular reference
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
        createdByUserId = nextGame.CreatedByUserId
    });
});

// ✅ כל המשחקים - ללא circular reference
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
            createdByUserId = g.CreatedByUserId,
            createdAt = g.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(games);
});

// ✅ משחקים שעברו - רק תאריכים בעבר
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

app.Run();
