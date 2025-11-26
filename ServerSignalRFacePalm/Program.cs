using ServerSignalRFacePalm.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<RoomBase>();
builder.Services.AddSingleton<PlayerBase>();

var app = builder.Build();

app.MapHub<MainHub>("/game");

app.Run();