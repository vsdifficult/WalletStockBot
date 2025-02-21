using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;

namespace WalletTelegramBot 
{
    public class WallBot
    {
        private static ITelegramBotClient _tgclient;
        private static ReceiverOptions _receiverOptions;
        private static IConfiguration _config;

        public WallBot(IConfiguration config)
        {
            _config = config;
        }

        public async Task StartAsync()
        {
            var botToken = _config["BotToken"];
            
            if (string.IsNullOrEmpty(botToken))
            {
                throw new Exception("Bot token is missing in configuration");
            }

            _tgclient = new TelegramBotClient(botToken);
            
            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };


            using var cts = new CancellationTokenSource();

            _tgclient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                _receiverOptions,
                cts.Token
            );


            var me = await _tgclient.GetMeAsync();
            Console.WriteLine($"Bot {me.Username} is running...");

            await Task.Delay(-1, cts.Token);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;
            
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            
            Console.WriteLine($"Received '{messageText}' in chat {chatId}");

            if (messageText.ToLower() == "/start")
            {
                var replyMarkup = CreateKeyboard();
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Welcome! \nThis bot send exchange rates\nPlease choose an option:",
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "I not found this command",
                    cancellationToken: cancellationToken);
            }
        }

        private ReplyKeyboardMarkup CreateKeyboard()
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "", "" }
            })
            {
                ResizeKeyboard = true
            };

            return keyboard;
        }


        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
} 