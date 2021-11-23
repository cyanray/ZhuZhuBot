using Cpdaily.CpdailyModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        /// <summary>
        /// 检查 SchoolAppCookie 是否可用（为 null 或超过 2 天未更新则为不可用）
        /// </summary>
        [NotMapped]
        [MemberNotNullWhen(true, nameof(SchoolAppCookie), nameof(SchoolAppCookieUpdateTime))]
        public bool IsSchoolAppCookieValid
        {
            get
            {
                if (SchoolAppCookieUpdateTime is null) return false;
                var diff = DateTime.Now - SchoolAppCookieUpdateTime;
                return SchoolAppCookie is not null && diff is not null && diff.Value.Days <= 2;
            }
        }

        public CpdailyLoginResult()
        {

        }

        public CpdailyLoginResult(LoginResult loginResult)
        {
            AuthId = loginResult.AuthId;
            Name = loginResult.Name;
            OpenId = loginResult.OpenId;
            PersonId = loginResult.PersonId;
            SessionToken = loginResult.SessionToken;
            TenantId = loginResult.TenantId;
            Tgc = loginResult.Tgc;
            UserId = loginResult.UserId;
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
