using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhuZhuBot.DbModels
{
    internal class User
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// QQ 号码
        /// </summary>
        [Required]
        [DisallowNull]
        public string? QId { get; set; }

        /// <summary>
        /// 学号
        /// </summary>
        public string? SchooldId { get; set; }

        /// <summary>
        /// 今日校园登录状态
        /// </summary>
        public CpdailyLoginResult? CpdailyLoginResult { get; set; }


        [NotMapped]
        [MemberNotNullWhen(true, member: nameof(CpdailyLoginResult))]
        public bool HasLoginResult 
        {
            get => CpdailyLoginResult is not null; 
        }
    }
}
