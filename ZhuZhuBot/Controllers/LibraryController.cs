﻿using Cpdaily.SchoolServices.Cqjtu.Library;
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
                var library = new CpdailyLibrary();
                var lib_cookie = await library.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                var logs = await library.GetReservationsAsync(lib_cookie);
                if (logs.Count != 0)
                {
                    await m.Reply(logs.Select(x => new PlainMessage($"{x.Id}. {x.LibraryName}, {x.Date:yyyy-MM-dd}"))
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
                Log.Error(ex, Constants.UnexpectedError);
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
                var library = new CpdailyLibrary();
                var lib_cookie = await library.LoginAsync(user.CpdailyLoginResult.SchoolAppCookie);
                var date = DateTime.Now;
                if (msg_str.Contains("明天"))
                {
                    date = DateTime.Now.AddDays(1);
                }
                await library.ReserveAsync(lib_cookie, $"{lib_name}馆", date);
                var logs = await library.GetReservationsAsync(lib_cookie);
                if (logs.Count != 0)
                {
                    await m.Reply(logs.Select(x => new PlainMessage($"{x.Id}. {x.LibraryName}, {x.Date:yyyy-MM-dd}"))
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
                Log.Error(ex, Constants.UnexpectedError);
            }

        }
    }
}