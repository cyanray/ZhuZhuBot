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

            Constants.AppDbContext.Database.Migrate();

            Constants.SecretKey = await Constants.CpdailyClient.GetSecretKeyAsync();
            var chk = Constants.SecretKey.Chk ?? "";
            Constants.SchoolDetails = await Constants.CpdailyClient.GetSchoolDetailsAsync("cqjtu", chk);

            try
            {
                Log.Information("读取配置文件中...");
                if (!File.Exists(Constants.ConfigFilePath))
                {
                    Log.Error("配置文件为空，已经生成默认配置文件");
                    File.WriteAllText(Constants.ConfigFilePath, JsonConvert.SerializeObject(new AppConfig()));
                    return;
                }
                var config_str = File.ReadAllText(Constants.ConfigFilePath);
                Constants.AppConfig = JsonConvert.DeserializeObject<AppConfig>(config_str);
                if (Constants.AppConfig is null || !Constants.AppConfig.IsValid())
                {
                    Log.Error("读取配置文件失败!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, Constants.UnexpectedError);
                return;
            }

            using var bot = new MiraiBot
            {
                Address = Constants.AppConfig.Address,
                VerifyKey = Constants.AppConfig.VerifyKey,
                QQ = Constants.AppConfig.BotQQ
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