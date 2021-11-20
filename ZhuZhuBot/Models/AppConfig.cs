using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhuZhuBot.Models
{
    internal class AppConfig
    {
        public string? BotQQ { get; set; } = null;

        public string? Hostname { get; set; } = "localhost";

        public int Port { get; set; } = 8080;

        public string? VerifyKey { get; set; } = null;

        [MemberNotNull(nameof(BotQQ), nameof(Hostname), nameof(VerifyKey))]
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(BotQQ)) throw new ArgumentNullException(nameof(BotQQ));
            if (string.IsNullOrEmpty(Hostname)) throw new ArgumentNullException(nameof(Hostname));
            if (string.IsNullOrEmpty(VerifyKey)) throw new ArgumentNullException(nameof(VerifyKey));
            return true;
        }

        [JsonIgnore]
        public string Address { get => $"{Hostname}:{Port}"; }
    }
}
