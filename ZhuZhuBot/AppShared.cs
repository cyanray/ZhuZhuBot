using Cpdaily;
using Cpdaily.CpdailyModels;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZhuZhuBot.Models;

namespace ZhuZhuBot
{
    internal class AppShared
    {
        public const string ConfigFilePath = "config.json";

        public const string DatabaseFilePath = "zzbot.db";

        public const string UnexpectedError = "Unexpected error.";

        public const string NotLoginMessage = "你尚未登录! 回复: “今日校园 登录 手机号码” 进行登录！";

        [NotNull]
        public static AppConfig? AppConfig { get; set; }

        public static AppDbContext AppDbContext { get; set; } = new AppDbContext();

        public static CpdailyClient CpdailyClient { get; set; } = new CpdailyClient();

        [NotNull]
        public static SecretKey? SecretKey { get; set; } = null;

        [NotNull]
        public static SchoolDetails? SchoolDetails { get; set; } = null;
    }
}
