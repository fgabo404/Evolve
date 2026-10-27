var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:8766");

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Run();
