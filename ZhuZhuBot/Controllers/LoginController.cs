using Microsoft.EntityFrameworkCore;
using Mirai.Net.Data.Messages;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZhuZhuBot.DbModels;

namespace ZhuZhuBot.Controllers
{
    internal class LoginController : IMiraiController
    {
        [MiraiMessageAction]
        public static async void LoginByBase64(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
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
                    var user = AppShared.AppDbContext.GetUserByQQ(m.GetSenderQQ());
                    if (user is null)
                    {
                        AppShared.AppDbContext.Users.Add(new User()
                        {
                            QId = m.GetSenderQQ(),
                            CpdailyLoginResult = login_result
                        });
                    }
                    else
                    {
                        user.CpdailyLoginResult = login_result;
                    }
                    await AppShared.AppDbContext.SaveChangesAsync();
                }
                await m.Reply("登录成功!");
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }

        }

        [MiraiMessageAction]
        public static async void SendLoginMessage(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                var match = Regex.Match(msg_str, @"今日校园[ ]*登录[ ]*(1(3\d|4[5-9]|5[0-35-9]|6[2567]|7[0-8]|8\d|9[0-35-9])\d{8})[ ]*([\d]+)?");
                if (!match.Success) return;
                if (match.Groups[3].Success) return;
                var phone = match.Groups[1].Value;
                if (string.IsNullOrEmpty(phone)) return;
                await AppShared.CpdailyClient.MobileLoginAsync(phone, AppShared.SecretKey);
                await m.Reply("已经发送短信验证码，请回复：“今日校园 登录 手机号码 验证码”，进行验证。");
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }
        }

        [MiraiMessageAction]
        public static async void VerifyLoginMessage(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                var match = Regex.Match(msg_str, @"今日校园[ ]*登录[ ]*(1(3\d|4[5-9]|5[0-35-9]|6[2567]|7[0-8]|8\d|9[0-35-9])\d{8})[ ]*([\d]+)");
                if (!match.Success) return;
                var phone = match.Groups[1].Value;
                var code = match.Groups[3].Value;
                if (string.IsNullOrEmpty(phone)) return;
                var login_result = await AppShared.CpdailyClient.MobileLoginAsync(phone, code, AppShared.SecretKey);

                var user = AppShared.AppDbContext.GetUserByQQ(m.GetSenderQQ());
                if (user is null)
                {
                    AppShared.AppDbContext.Users.Add(new User()
                    {
                        QId = m.GetSenderQQ(),
                        CpdailyLoginResult = new CpdailyLoginResult(login_result)
                    });
                }
                else
                {
                    user.CpdailyLoginResult = new CpdailyLoginResult(login_result);
                }
                await AppShared.AppDbContext.SaveChangesAsync();
                await m.Reply("登录成功!");
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }
        }

    }
}
