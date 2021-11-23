using Cpdaily.SchoolServices.Cqjtu.NetPay;
using Microsoft.EntityFrameworkCore;
using Mirai.Net.Data.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhuZhuBot.Controllers
{
    public class PersonController : IMiraiController
    {
        [MiraiMessageAction]
        public static async void Hello(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                if (msg_str != "个人信息" && msg_str != "你好") return;
                var user = Constants.AppDbContext.Users
                        .AsNoTracking()
                        .Where(u => u.QId == m.GetQQ())
                        .Include(u => u.CpdailyLoginResult)
                        .FirstOrDefault();
                if (user is null || user.CpdailyLoginResult is null)
                {
                    await m.Reply("你尚未登录! 回复: “今日校园 登录 手机号码” 进行登录！");
                    return;
                }
                var user_info = await Constants.CpdailyClient.GetUserInfoAsync(user.CpdailyLoginResult.ToLoginResult());
                if (user_info is not null)
                {
                    await m.Reply($"你好，{user_info.Name}！");
                }
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, Constants.UnexpectedError);
            }

        }

        [MiraiMessageAction]
        public static async void CardInfo(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                if (msg_str != "余额" && msg_str != "一卡通" && msg_str != "一卡通余额") return;
                var user = Constants.AppDbContext.Users
                        .Where(u => u.QId == m.GetQQ())
                        .Include(u => u.CpdailyLoginResult)
                        .FirstOrDefault();
                if (user is null || user.CpdailyLoginResult is null)
                {
                    await m.Reply("你尚未登录! 回复: “今日校园 登录 手机号码” 进行登录！");
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
                await m.Reply($"你的一卡通余额：￥{info.RemainingAmount} (待充值: ￥{info.UnaccountedAmount})");
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, Constants.UnexpectedError);
            }

        }
    }
}
