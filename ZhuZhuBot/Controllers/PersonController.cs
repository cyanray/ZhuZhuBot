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
                var user = AppShared.AppDbContext.Users
                        .AsNoTracking()
                        .Where(u => u.QId == m.GetSenderQQ())
                        .Include(u => u.CpdailyLoginResult)
                        .FirstOrDefault();
                if (user is null || !user.HasLoginResult)
                {
                    await m.Reply(AppShared.NotLoginMessage);
                    return;
                }
                var user_info = await AppShared.CpdailyClient.GetUserInfoAsync(user.CpdailyLoginResult.ToLoginResult());
                if (user_info is not null)
                {
                    await m.Reply($"你好，{user_info.Name}！");
                }
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
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
                var user = AppShared.AppDbContext.GetUserByQQ(m.GetSenderQQ());
                if (user is null || !user.HasLoginResult)
                {
                    await m.Reply(AppShared.NotLoginMessage);
                    return;
                }
                if (user.CpdailyLoginResult.SchoolAppCookie is null)
                {
                    var cookie = await AppShared.CpdailyClient.UserStoreAppListAsync(
                            user.CpdailyLoginResult.ToLoginResult(), AppShared.SchoolDetails);
                    user.CpdailyLoginResult.UpdateSchoolAppCookie(cookie);
                    await AppShared.AppDbContext.SaveChangesAsync();
                }
                var payClient = new NetPay();
                var pay_cookie = await payClient.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                var info = await payClient.GetAccountInfoAsync(pay_cookie);
                await m.Reply($"你的一卡通余额：￥{info.RemainingAmount} (待充值: ￥{info.UnaccountedAmount})");
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }

        }
    }
}
