using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using BCrypt;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PostDb>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("localeDb")));
builder.Services.AddDbContext<UserDb>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("localeDb")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

////////// POSTS /////////////

//GET ALL
app.MapGet("/api/posts", async (PostDb db) =>
    await db.Posts.ToListAsync());

//GET ONE
app.MapGet("/api/posts/{id}", async (int PostId, PostDb db) =>
    await db.Posts.FindAsync(PostId) 
            is Post post
            ? Results.Ok(post)
            : Results.NotFound());

// POST
app.MapPost("/api/posts", async (Post post, PostDb db) => 
{
    post.Date = DateTime.Now;
    db.Posts.Add(post);
    await db.SaveChangesAsync();
    return Results.Created($"/api/posts/{post.PostId}", post);
});

// PUT
app.MapPut("/api/posts/{id}", async (int PostId, Post inputPost, PostDb db) =>
{
    var post = await db.Posts.FindAsync(PostId);

    if (post is null) return Results.NotFound();

    post.Title = inputPost.Title;
    post.Image = inputPost.Image;
    post.Date = inputPost.Date;
    post.Location = inputPost.Location;
    post.UpVotes = inputPost.UpVotes;
    post.DownVotes = inputPost.DownVotes;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

// DELETE
app.MapDelete("/api/posts/{id}", async (int PostId, PostDb db) =>
{
    if (await db.Posts.FindAsync(PostId) is Post post)
    {
        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return Results.Ok(post);
    }

    return Results.NotFound();
});

////////// USERS /////////////

// GET ONE
app.MapGet("/api/users/{id}", async (int UserId, UserDb db) => 
    await db.Users.FindAsync(UserId)
            is User user
            ? Results.Ok(user)
            : Results.NotFound());

// POST 
app.MapPost("/api/users", async (User user, UserDb db) =>
{
    string salt = BCrypt.Net.BCrypt.GenerateSalt();
    string hash = BCrypt.Net.BCrypt.HashPassword(user.Password, salt);
    user.Password = hash;

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.UserId}", user);
});

// POST (authenticate user)
app.MapPost("/api/userauth", async ( User inputUser, UserDb db) =>
{
    bool isUser = await db.Users.FindAsync(inputUser.Username) is User;
    if( isUser )
    {
        var user = await db.Users.FindAsync(inputUser.Username);
        if(BCrypt.Net.BCrypt.Verify(inputUser.Password, user.Password ))
        {
            Results.Ok(user);
        }else{
            Results.Text("Incorrect password");
        }
    }else{
        Results.NotFound();
    }
    

});

app.Run();

[Keyless]
[Index(nameof(Username), IsUnique = true)]
class User {
    public int UserId { get; set; }

    [Required]
    [StringLength(30)]
    public string Username { get; set; }
    public int Rep { get; set; }
    public string Password { get; set; }

    public List<int>? PId { get; set; }

}

class Post {
    public int PostId { get; set; }
    public string? Title {get; set; }
    public DateTime Date { get; set; }
    public string Image { get; set; }
    public string? Location { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public string Author { get; set; }
}

class PostDb : DbContext 
{
    public PostDb(DbContextOptions<PostDb> options) : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();
}

class UserDb : DbContext {
    public UserDb(DbContextOptions<UserDb> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}


// EXAMPLES
// app.MapGet("/todoitems", async (TodoDb db) =>
//     await db.Todos.ToListAsync());

// app.MapGet("/todoitems/complete", async (TodoDb db) =>
//     await db.Todos.Where(t => t.IsComplete).ToListAsync());

// app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
//     await db.Todos.FindAsync(id)
//         is Todo todo
//             ? Results.Ok(todo)
//             : Results.NotFound());

// app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
// {
//     db.Todos.Add(todo);
//     await db.SaveChangesAsync();

//     return Results.Created($"/todoitems/{todo.Id}", todo);
// });

// app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
// {
//     var todo = await db.Todos.FindAsync(id);

//     if (todo is null) return Results.NotFound();

//     todo.Name = inputTodo.Name;
//     todo.IsComplete = inputTodo.IsComplete;

//     await db.SaveChangesAsync();

//     return Results.NoContent();
// });

// app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
// {
//     if (await db.Todos.FindAsync(id) is Todo todo)
//     {
//         db.Todos.Remove(todo);
//         await db.SaveChangesAsync();
//         return Results.Ok(todo);
//     }

//     return Results.NotFound();
// });

// class Todo
// {
//     public int Id { get; set; }
//     public string Name { get; set; }
//     public bool IsComplete { get; set; }
// }

// class TodoDb : DbContext
// {
//     public TodoDb(DbContextOptions<TodoDb> options)
//         : base(options) { }

//     public DbSet<Todo> Todos => Set<Todo>();
// }


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateTime.Now.AddDays(index),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");

// app.Run();

// record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }