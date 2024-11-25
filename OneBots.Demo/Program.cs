using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneBot;
using OneBot.Attributes;
using OneBot.Base;
using OneBot.Interfaces;
using OneBot.Tg;
using System.Reflection;

ContextBot<BaseUser, DataBase> bot = new();

Dictionary<string, string> config = new()
{
    { TgClient.KeySettingTOKEN, "yourKey" }
};

bot.Init(
    dbBuild => dbBuild.UseSqlite("Data Source=database.db"),
    configuration => configuration.AddInMemoryCollection(config!),
    servicesDetect: [Assembly.GetAssembly(typeof(DataBase))!, Assembly.GetAssembly(typeof(TgClient))!]
);

var tgClient = bot.GetService<TgClient<BaseUser, DataBase>>();

tgClient.RegisterUpdateHadler(i =>
{
    if(i.Message == null) return;
    i.Send(i.Message);
});

await tgClient.Run();

[ServiceDB]
public class DataBase(DbContextOptions options) : UsersDB<BaseUser>(options), IDBTg<BaseUser>
{
    public DbSet<TgUser<BaseUser>> TgUsers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigurateDBTg<BaseUser>();
    }
}

[Service]
public class Logger : ILogger
{
    public void Log(string message, ILogger.LogTypes logTypes = ILogger.LogTypes.Info, int? senderId = null)
    {
        switch (logTypes)
        {
            case ILogger.LogTypes.Warning:
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                break;
            case ILogger.LogTypes.Error:
                Console.ForegroundColor= ConsoleColor.DarkRed;
                break;
        }
        Console.WriteLine($"[{logTypes}]({senderId}) -> {message}");
        Console.ResetColor();
    }
}