using System;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Message.Entity;
using Lagrange.Core.Event;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface.Api;
using System.Drawing;
using Lagrange.Core.Message;
using System.Net.Sockets;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Utility.Sign;
using System.Security.Cryptography;
using System.Net.Http;

namespace QQBotConsole
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        static async Task Main(string[] args)
        {
            // 实例化 QQ 机器人
            BotDeviceInfo _deviceInfo = new()
            {
                Guid = Guid.NewGuid(),
                MacAddress = GenRandomBytes(6),
                DeviceName = $"Lagrange-52D02F",
                SystemKernel = "Windows 10.0.19042",
                KernelVersion = "10.0.19042.0"
            };
            BotKeystore _keyStore = new BotKeystore();
            BotConfig _config = new BotConfig
            {
                UseIPv6Network = false,
                GetOptimumServer = true,
                AutoReconnect = true,
                Protocol = Protocols.Linux,
                CustomSignProvider = new QQSigner(),
            };
            var bot = BotFactory.Create(_config, _deviceInfo, _keyStore);
            var botClient = new BotClient(_config, bot);



            bot.Invoker.OnGroupMessageReceived += async (context, e) =>
            {
                LogBotEvent(context, e, $"{e.Chain.ToPreviewString()}");
                string url = "";
                foreach (var entity in e.Chain)
                {
                    if (entity is ImageEntity ImageEntity)
                    {
                        if (e.Chain.GroupUin.HasValue && e.Chain.FriendUin != 1498709237 && e.Chain.GroupUin == 891521098)
                        {
                            var groupMessageChain = MessageBuilder.Group(e.Chain.GroupUin.Value);
                            string localPath = await DownloadImageAsync(ImageEntity.ImageUrl);
                            groupMessageChain.Image(localPath);
                            groupMessageChain.Mention(e.Chain.FriendUin);
                            var result = await bot.SendMessage(groupMessageChain.Build());
                        }
                    }

                    if (entity is TextEntity Text)
                    {
                        if (e.Chain.GroupUin.HasValue && e.Chain.FriendUin != 1498709237 && e.Chain.GroupUin == 891521098)
                        {
                            var groupMessageChain = MessageBuilder.Group(e.Chain.GroupUin.Value);
                            groupMessageChain.Text(Text.Text);
                            groupMessageChain.Mention(e.Chain.FriendUin);
                            var result = await bot.SendMessage(groupMessageChain.Build());
                        }
                    }
                }

            };

            bot.Invoker.OnBotOnlineEvent += (context, e) =>
            {
                LogBotEvent(context, e, e.EventMessage);
                bot.UpdateKeystore();
            };

            bot.Invoker.OnFriendMessageReceived += (context, e) =>
            {
                LogBotEvent(context, e, $"{e.Chain.ToPreviewString()}");

            };
            bot.Invoker.OnFriendMessageReceived += async (context, e) => await OnFriendMessage(context, e);

            // 登录
            await botClient.LoginAsync();

            // 保持程序运行
            await Task.Delay(-1);
        }

        public static byte[] GenRandomBytes(int length)
        {
            byte[] randomBytes = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public static async Task OnFriendMessage(BotContext bot, FriendMessageEvent args)
        {
            Console.WriteLine("OnFriendMessage");
        }

        private static void LogBotEvent(BotContext context, EventBase e, string message)
        {

            Console.WriteLine($"[{context.BotUin}][{e.EventTime:yyyy-MM-dd HH:mm:ss}]{message}");

        }

        private static async Task<string> DownloadImageAsync(string imageUrl)
        {
            string fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            string localPath = Path.Combine("images", fileName);

            Directory.CreateDirectory("images");

            using (var response = await httpClient.GetAsync(imageUrl))
            using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write))
            {
                await response.Content.CopyToAsync(fs);
            }

            return localPath;
        }

        public class BotClient
        {
            private BotConfig _config;
            private BotContext? _bot;
            private MessageBuilder groupMessageChain;
            private MessageBuilder privateMessageChain;

            public event Func<LoginSuccessEventArgs, Task> OnLoginSuccess;
            public event Func<MessageReceivedEventArgs, Task> OnMessageReceived;

            public BotClient(BotConfig config, BotContext? bot)
            {
                _config = config;
                _bot = bot;
            }

            public void BuildGroupMessage(uint _uint)
            {
                groupMessageChain = MessageBuilder.Group(_uint);
            }

            public void BuildPrivateMessage(uint _uint)
            {
                privateMessageChain = MessageBuilder.Friend(_uint);
            }

            public MessageBuilder AddPrivateTextMessage(string content)
            {
                privateMessageChain.Text(content);
                return privateMessageChain;
            }

            public void AnaMessageChain(MessageChain messageChain)
            {
                foreach (var entity in messageChain)
                {
                    if (entity is TextEntity textEntity)
                        Console.WriteLine(textEntity.Text);
                }
            }

            public async Task<MessageResult?> SendPrivateMessage()
            {
                var result = await _bot.SendMessage(privateMessageChain.Build());
                return result;
            }

            public async Task<MessageResult?> SendPublicMessage()
            {
                var result = await _bot.SendMessage(groupMessageChain.Build());
                return result;
            }

            public MessageBuilder AddGroupTextMessage(string content)
            {
                groupMessageChain.Text(content);
                return groupMessageChain;
            }

            public async Task LoginAsync()
            {
                Console.WriteLine("Hello, This is Lagrange. Version: Latest stable 0.2.1");
                Console.WriteLine("Scan Image.png to log in");

                // 模拟二维码获取过程
                var qrCode = await _bot.FetchQrCode();
                if (qrCode != null)
                {
                    await File.WriteAllBytesAsync("qr.png", qrCode.Value.QrCode);
                    await _bot.LoginByQrCode();
                    Console.WriteLine("Hello!");
                }



            }

            public async Task SendPrivateMessageAsync(long userId, string message)
            {
                // 发送私聊消息
                await Task.Delay(500); // 模拟网络延迟
                Console.WriteLine($"发送私聊消息给 {userId}：{message}");
            }

            public async Task SendGroupMessageAsync(long groupId, string message)
            {
                // 发送群消息
                await Task.Delay(500); // 模拟网络延迟
                Console.WriteLine($"发送群消息到群 {groupId}：{message}");
            }

        }

        public class LoginSuccessEventArgs : EventArgs
        {
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public IMessage Message { get; set; }
        }

        public interface IMessage
        {
            string Content { get; set; }
        }

        public class PrivateMessage : IMessage
        {
            public string Content { get; set; }
            public long UserId { get; set; }
        }

        public class GroupMessage : IMessage
        {
            public string Content { get; set; }
            public long GroupId { get; set; }
        }

    }
}