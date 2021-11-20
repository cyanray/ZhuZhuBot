using Cpdaily.CpdailyModels;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhuZhuBot.DbModels
{
    internal class CpdailyLoginResult
    {
        public int Id { get; set; }

        public string? AuthId { get; set; }

        public string? Name { get; set; }

        public string? OpenId { get; set; }

        public string? PersonId { get; set; }

        public string? SessionToken { get; set; }

        public string? TenantId { get; set; }

        public string? Tgc { get; set; }

        public string? UserId { get; set; }

        /// <summary>
        /// 使用 UserStoreAppList 获取的用于访问校内 APP 的 Cookie
        /// </summary>
        public string? SchoolAppCookie { get; set; }

        /// <summary>
        /// SchoolAppCookie 的更新时间(易过期，隔一段时间需要重新获取)
        /// </summary>
        public DateTime? SchoolAppCookieUpdateTime { get; set; } = null;

        [MemberNotNull(nameof(SchoolAppCookie), nameof(SchoolAppCookieUpdateTime))]
        public void UpdateSchoolAppCookie(string cookie)
        {
            SchoolAppCookie = cookie;
            SchoolAppCookieUpdateTime = DateTime.Now;
        }

        public LoginResult ToLoginResult()
        {
            return new LoginResult()
            {
                AuthId = AuthId,
                Name = Name,
                OpenId = OpenId,
                PersonId = PersonId,
                SessionToken = SessionToken,
                TenantId = TenantId,
                Tgc = Tgc,
                UserId = UserId
            };
        }

    }
}
