using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZhuZhuBot
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    sealed class MiraiMessageActionAttribute : Attribute
    {

    }

}
