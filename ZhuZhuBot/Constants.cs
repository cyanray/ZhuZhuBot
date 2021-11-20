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
    internal class Constants
    {
        public const string ConfigFilePath = "config.json";

        public const string DatabaseFilePath = "zzbot.db";

        public const string UnexpectedError = "Unexpected error.";

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
