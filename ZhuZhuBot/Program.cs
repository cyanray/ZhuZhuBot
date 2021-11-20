

using AHpx.Extensions.StringExtensions;
using Microsoft.EntityFrameworkCore;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZhuZhuBot.Models;
using ZhuZhuBot.DbModels;
using Cqjtu = Cpdaily.SchoolServices.Cqjtu;
using Cpdaily.SchoolServices.Cqjtu.NetPay;

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

            bot.MessageReceived
                .OfType<MessageReceiverBase>()
                .Subscribe(async x =>
                {
                    try
                    {
                        var msg_str = x.MessageChain.GetAllPlainText();
                        if (string.IsNullOrEmpty(msg_str)) return;
                        var match = Regex.Match(msg_str, @"今日校园[ ]*base64[ ]*([\S]+)");
                        if (!match.Success) return;
                        var base64 = match.Groups[1].Value;
                        var json_str = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        var login_result = JsonConvert.DeserializeObject<CpdailyLoginResult>(json_str);
                        if (login_result is null)
                        {
                            Log.Warning("解码 LoginResult Base64 失败: {base64}", base64);
                        }
                        else
                        {
                            Constants.AppDbContext.Users.Add(new User()
                            {
                                QId = x.GetQQ(),
                                CpdailyLoginResult = login_result
                            });
                            await Constants.AppDbContext.SaveChangesAsync();
                        }
                        await x.Reply("登录成功!");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, Constants.UnexpectedError);
                    }
                });

            bot.MessageReceived
                .OfType<MessageReceiverBase>()
                .Subscribe(async x =>
                {
                    try
                    {
                        var msg_str = x.MessageChain.GetAllPlainText();
                        if (string.IsNullOrEmpty(msg_str)) return;
                        if (msg_str != "个人信息") return;
                        var user = Constants.AppDbContext.Users
                                .AsNoTracking()
                                .Where(u => u.QId == x.GetQQ())
                                .Include(u => u.CpdailyLoginResult)
                                .FirstOrDefault();
                        if (user is null || user.CpdailyLoginResult is null)
                        {
                            await x.Reply("你尚未登录! 回复: “今日校园 登录 手机号码” 进行登录！");
                            return;
                        }
                        var user_info = await Constants.CpdailyClient.GetUserInfoAsync(user.CpdailyLoginResult.ToLoginResult());
                        if (user_info is not null)
                        {
                            await x.Reply(JsonConvert.SerializeObject(user_info));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, Constants.UnexpectedError);
                    }
                });

            bot.MessageReceived
                .OfType<MessageReceiverBase>()
                .Subscribe(async x =>
                {
                    try
                    {
                        var msg_str = x.MessageChain.GetAllPlainText();
                        if (string.IsNullOrEmpty(msg_str)) return;
                        if (msg_str != "余额" && msg_str != "一卡通" && msg_str != "一卡通余额") return;
                        var user = Constants.AppDbContext.Users
                                .Where(u => u.QId == x.GetQQ())
                                .Include(u => u.CpdailyLoginResult)
                                .FirstOrDefault();
                        if (user is null || user.CpdailyLoginResult is null)
                        {
                            await x.Reply("你尚未登录! 回复: “今日校园 登录 手机号码” 进行登录！");
                            return;
                        }
                        if (user.CpdailyLoginResult.SchoolAppCookie is null)
                        {
                            var cookie = await Constants.CpdailyClient.UserStoreAppListAsync(
                                    user.CpdailyLoginResult.ToLoginResult(), Constants.SchoolDetails);
                            user.CpdailyLoginResult.UpdateSchoolAppCookie(cookie);
                            await Constants.AppDbContext.SaveChangesAsync();
                        }
                        var payClient = new NetPay();
                        var pay_cookie = await payClient.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                        var info = await payClient.GetAccountInfoAsync(pay_cookie);
                        await x.Reply($"你的一卡通余额：￥{info.RemainingAmount} (待充值: ￥{info.UnaccountedAmount})");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, Constants.UnexpectedError);
                    }
                });

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