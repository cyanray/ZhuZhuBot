using Cpdaily.SchoolServices.Cqjtu.Library;
using Microsoft.EntityFrameworkCore;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZhuZhuBot.Controllers
{
    internal class LibraryController : IMiraiController
    {
        [MiraiMessageAction]
        public static async void MyReservation(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                if (msg_str != "图书馆" && msg_str != "我的预约") return;
                var user = AppShared.AppDbContext.GetUserByQQ(m.GetSenderQQ());
                if (user is null || !user.HasLoginResult)
                {
                    await m.Reply(AppShared.NotLoginMessage);
                    return;
                }
                if (!user.CpdailyLoginResult.IsSchoolAppCookieValid)
                {
                    var cookie = await AppShared.CpdailyClient.UserStoreAppListAsync(
                            user.CpdailyLoginResult.ToLoginResult(), AppShared.SchoolDetails);
                    user.CpdailyLoginResult.UpdateSchoolAppCookie(cookie);
                    await AppShared.AppDbContext.SaveChangesAsync();
                }
                var library = new CpdailyLibrary();
                var lib_cookie = await library.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                var logs = await library.GetReservationsAsync(lib_cookie);
                logs = logs.OrderBy(x => x.Date).ToList();
                if (logs.Count != 0)
                {
                    await m.Reply(logs.Select(x => new PlainMessage($"{x.LibraryName}, {x.Date:yyyy-MM-dd}, {x.Id}\n"))
                                      .Select(x => (MessageBase)x)
                                      .ToArray());
                }
                else
                {
                    await m.Reply("没有找到图书馆预约记录！");
                }
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }

        }

        [MiraiMessageAction]
        public static async void Reserve(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                var match = Regex.Match(msg_str, @"预约图书馆[ ]*(南岸|双福)(馆)?");
                if (!match.Success) return;
                var lib_name = match.Groups[1].Value;

                var user = AppShared.AppDbContext.GetUserByQQ(m.GetSenderQQ());
                if (user is null || !user.HasLoginResult)
                {
                    await m.Reply(AppShared.NotLoginMessage);
                    return;
                }
                if (!user.CpdailyLoginResult.IsSchoolAppCookieValid)
                {
                    var cookie = await AppShared.CpdailyClient.UserStoreAppListAsync(
                            user.CpdailyLoginResult.ToLoginResult(), AppShared.SchoolDetails);
                    user.CpdailyLoginResult.UpdateSchoolAppCookie(cookie);
                    await AppShared.AppDbContext.SaveChangesAsync();
                }
                var library = new CpdailyLibrary();
                var lib_cookie = await library.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                var date = DateTime.Now;
                if (msg_str.Contains("明天"))
                {
                    date = DateTime.Now.AddDays(1);
                }
                await library.ReserveAsync(lib_cookie, $"{lib_name}馆", date);
                var logs = await library.GetReservationsAsync(lib_cookie);
                logs = logs.OrderBy(x => x.Date).ToList();
                if (logs.Count != 0)
                {
                    await m.Reply(logs.Select(x => new PlainMessage($"{x.LibraryName}, {x.Date:yyyy-MM-dd}, {x.Id}\n"))
                                      .Select(x => (MessageBase)x)
                                      .ToArray());
                }
                else
                {
                    await m.Reply("没有找到图书馆预约记录！似乎预约失败了！");
                }
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }

        }

        [MiraiMessageAction]
        public static async void CancelReserve(MessageReceiverBase m)
        {
            try
            {
                var msg_str = m.MessageChain.GetAllPlainText();
                if (string.IsNullOrEmpty(msg_str)) return;
                var match = Regex.Match(msg_str, @"取消预约[ ]*(今天|明天)");
                if (!match.Success) return;

                var user = AppShared.AppDbContext.GetUserByQQ(m.GetSenderQQ());
                if (user is null || !user.HasLoginResult)
                {
                    await m.Reply(AppShared.NotLoginMessage);
                    return;
                }
                if (!user.CpdailyLoginResult.IsSchoolAppCookieValid)
                {
                    var cookie = await AppShared.CpdailyClient.UserStoreAppListAsync(
                            user.CpdailyLoginResult.ToLoginResult(), AppShared.SchoolDetails);
                    user.CpdailyLoginResult.UpdateSchoolAppCookie(cookie);
                    await AppShared.AppDbContext.SaveChangesAsync();
                }
                var library = new CpdailyLibrary();
                var lib_cookie = await library.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                var logs = await library.GetReservationsAsync(lib_cookie);
                logs = logs.OrderBy(x => x.Date).ToList();
                if (logs.Count == 0)
                {
                    await m.Reply("没有找到图书馆预约记录！");
                    return;
                }
                var date = DateTime.Now;
                if (msg_str.Contains("明天"))
                {
                    date = DateTime.Now.AddDays(1);
                }
                var log = logs.Where(x => x.Date.Date == date.Date).FirstOrDefault();
                if (log == null)
                {
                    await m.Reply("你在当天没有预约记录！");
                    return;
                }
                if (log.Id is null)
                {
                    await m.Reply("无法取消当天的预约：ID 不存在！");
                    return;
                }
                await library.CancelReservationsAsync(lib_cookie, log.Id);
                logs = await library.GetReservationsAsync(lib_cookie);
                logs = logs.OrderBy(x => x.Date).ToList();
                if (logs.Count != 0)
                {
                    await m.Reply(logs.Select(x => new PlainMessage($"{x.LibraryName}, {x.Date:yyyy-MM-dd}, {x.Id}\n"))
                                      .Select(x => (MessageBase)x)
                                      .ToArray());
                }
                else
                {
                    await m.Reply("成功取消图书馆预约！");
                }
            }
            catch (Exception ex)
            {
                m.TryReply(ex.Message);
                Log.Error(ex, AppShared.UnexpectedError);
            }

        }

    }
}
