using Microsoft.EntityFrameworkCore;
using Mirai.Net.Sessions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Reactive.Linq;
using ZhuZhuBot.Models;

namespace ZhuZhuBot
{
    public static class Program
    {
        static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: SystemConsoleTheme.Colored)
                .CreateLogger();

            AppShared.AppDbContext.Database.Migrate();

            AppShared.SecretKey = await AppShared.CpdailyClient.GetSecretKeyAsync();
            var chk = AppShared.SecretKey.Chk ?? "";
            AppShared.SchoolDetails = await AppShared.CpdailyClient.GetSchoolDetailsAsync("cqjtu", chk);

            try
            {
                Log.Information("读取配置文件中...");
                if (!File.Exists(AppShared.ConfigFilePath))
                {
                    Log.Error("配置文件为空，已经生成默认配置文件");
                    File.WriteAllText(AppShared.ConfigFilePath, JsonConvert.SerializeObject(new AppConfig()));
                    return;
                }
                var config_str = File.ReadAllText(AppShared.ConfigFilePath);
                AppShared.AppConfig = JsonConvert.DeserializeObject<AppConfig>(config_str);
                if (AppShared.AppConfig is null || !AppShared.AppConfig.IsValid())
                {
                    Log.Error("读取配置文件失败!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, AppShared.UnexpectedError);
                return;
            }

            using var bot = new MiraiBot
            {
                Address = AppShared.AppConfig.Address,
                VerifyKey = AppShared.AppConfig.VerifyKey,
                QQ = AppShared.AppConfig.BotQQ
            };


            var connect = async () =>
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("尝试建立连接...");
                        await bot.LaunchAsync();
                        Console.WriteLine("Bot working...");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    await Task.Delay(1000);
                }
            };

            // 与 mah 建立连接
            await connect();

            // 处理各种事件
            bot.DisconnectionHappened
                .Subscribe(async status =>
                {
                    Console.WriteLine($"失去连接:{status}");
                    await connect();
                });

            bot.AddAllMiraiMessageActions();

            while (true)
            {
                string? cmd = Console.ReadLine();
                if (!string.IsNullOrEmpty(cmd))
                {
                    if (cmd is "quit" or "exit" or "q") return;
                }
            }

        }
    }

}