using Microsoft.EntityFrameworkCore;
using KickRateServer.Data;
using KickRateServer.Models;
using KickRateServer.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Data/kickrate.db"));

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

app.MapPost("/auth/register", async (RegisterDto dto, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
    {
        return Results.BadRequest(new { message = "שם המשתמש כבר קיים" });
    }

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    var user = new User
    {
        Username = dto.Username,
        PasswordHash = passwordHash,
        CreatedAt = DateTime.Now
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        message = "ההרשמה בוצעה בהצלחה"
    });
});

app.MapPost("/auth/login", async (LoginDto dto, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

    if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
    {
        return Results.BadRequest(new { message = "שם משתמש או סיסמה שגויים" });
    }

    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        message = "התחברת בהצלחה"
    });
});

app.MapGet("/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Select(u => new { u.Id, u.Username, u.CreatedAt })
        .ToListAsync();
    return Results.Ok(users);
});

app.MapGet("/games", async (AppDbContext db) =>
{
    var games = await db.Games.Include(g => g.CreatedByUser).ToListAsync();
    return Results.Ok(games);
});

app.MapPost("/games", async (CreateGameDto dto, AppDbContext db) =>
{
    var game = new Game
    {
        GameDate = dto.GameDate,
        GameTime = dto.GameTime,
        Location = dto.Location,
        Opponent = dto.Opponent,
        CreatedByUserId = dto.CreatedByUserId
    };

    db.Games.Add(game);
    await db.SaveChangesAsync();

    return Results.Created($"/games/{game.Id}", game);
});

app.Run();
