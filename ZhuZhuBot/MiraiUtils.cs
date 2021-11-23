using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using System.Reactive.Linq;
using System.Reflection;
using ZhuZhuBot.Controllers;

namespace ZhuZhuBot
{
    public static class MiraiUtils
    {

        public static IEnumerable<T> GetAll<T>(this IEnumerable<MessageBase> messageChain)
        {
            return messageChain.Where(x => x is T).Select(x => (T)(object)x);
        }

        public static string? GetAllPlainText(this IEnumerable<MessageBase> messageChain, string separator = "")
        {
            return string.Join(separator, messageChain.GetAll<PlainMessage>().Select(x => x.Text));
        }

        public static T? GetFirst<T>(this IEnumerable<MessageBase> messageChain)
        {
            return messageChain.GetAll<T>().FirstOrDefault();
        }

        public static Task<string> Reply(this FriendMessageReceiver m, params MessageBase[] messages)
        {
            return MessageManager.SendFriendMessageAsync(m.Sender, messages);
        }

        public static Task<string> Reply(this GroupMessageReceiver m, params MessageBase[] messages)
        {
            return MessageManager.SendGroupMessageAsync(m.Sender.Group.Id, messages);
        }

        public static Task<string> Reply(this TempMessageReceiver m, params MessageBase[] messages)
        {
            return MessageManager.SendTempMessageAsync(m.Sender.Id, m.Sender.Group.Id, messages);
        }

        public static Task<string> Reply(this MessageReceiverBase m, params MessageBase[] messages)
        {
            return m switch
            {
                FriendMessageReceiver f => f.Reply(messages),
                GroupMessageReceiver g => g.Reply(messages),
                TempMessageReceiver t => t.Reply(messages),
                _ => throw new NotImplementedException($"对 {m.Type} 的 Reply 方法尚未实现!"),
            };
        }

        public static string GetQQ(this MessageReceiverBase m)
        {
            return m switch
            {
                FriendMessageReceiver f => f.Sender.Id,
                GroupMessageReceiver g => g.Sender.Id,
                TempMessageReceiver t => t.Sender.Id,
                _ => throw new NotImplementedException($"对 {m.Type} 的 GetQQ 方法尚未实现!"),
            };
        }

        public static Task<string> Reply(this FriendMessageReceiver m, string message) => m.Reply(new PlainMessage(message));

        public static Task<string> Reply(this GroupMessageReceiver m, string message) => m.Reply(new PlainMessage(message));

        public static Task<string> Reply(this TempMessageReceiver m, string message) => m.Reply(new PlainMessage(message));

        public static Task<string> Reply(this MessageReceiverBase m, string message) => m.Reply(new PlainMessage(message));

        public static async void TryReply(this FriendMessageReceiver m, string message)
        {
            try
            {
                await m.Reply(new PlainMessage(message));
            }
            catch { }
        }
        public static async void TryReply(this GroupMessageReceiver m, string message)
        {
            try
            {
                await m.Reply(new PlainMessage(message));
            }
            catch { }
        }

        public static async void TryReply(this TempMessageReceiver m, string message)
        {
            try
            {
                await m.Reply(new PlainMessage(message));
            }
            catch { }
        }

        public static async void TryReply(this MessageReceiverBase m, string message)
        {
            try
            {
                await m.Reply(new PlainMessage(message));
            }
            catch { }
        }

        public static void AddAllMiraiMessageActions(this MiraiBot bot)
        {
            var assembly = Assembly.GetAssembly(typeof(IMiraiController));
            if (assembly is null) return;
            var miraiMessageActions = assembly
                  .GetTypes()
                  .SelectMany(t => t.GetMethods())
                  .Where(m => m.GetCustomAttributes(typeof(MiraiMessageActionAttribute), false).Length > 0)
                  .ToList();
            if (miraiMessageActions == null) return;
            foreach (var method in miraiMessageActions)
            {
                var messageType = method.GetParameters().Select(x => x.ParameterType).FirstOrDefault();
                if (messageType == null) continue;
                bot.MessageReceived
                    .Where(x => messageType.IsAssignableFrom(x.GetType()))
                    .Subscribe(x =>
                    {
                        method.Invoke(null, new object[] { x });
                    });
            }
        }

    }
}
