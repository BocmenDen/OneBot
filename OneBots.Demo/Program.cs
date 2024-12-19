using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneBot;
using OneBot.Base;
using OneBot.Tg;
using OneBots.Demo;
using System.Reflection;

IHost host = BotBuilder.CreateDefaultBuilder()
    .ConfigureAppConfiguration(app => app.AddInMemoryCollection([new(TgClient.KeySettingTOKEN, "ВАШ ТОКЕН")]))
    .RegisterDBContextOptions(b => b.UseSqlite("Data Source=database.db"))
    .RegisterServices(
        Assembly.GetAssembly(typeof(Program)),
        Assembly.GetAssembly(typeof(TgClient))
    )
    .Build();

IServiceProvider service = host.Services;

var tgClient = service.GetRequiredService<TgClient<BaseUser, DataBase>>();
var spamFilter = host.Services.GetRequiredService<MessageSpam<BaseUser>>();
tgClient.RegisterUpdateHadler(spamFilter.HandleCommand);

spamFilter.Init(async (context) =>
{
    await context.Send(context.Update.Message ?? "Вы отправили пустое сообщение");
});
tgClient.Run().Wait();