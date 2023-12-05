using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegrambot.Services
{
    public class HandleUpdateService
    {
        private readonly ILogger<HandleUpdateService> _logger;
        private readonly ITelegramBotClient _botClient;

        public HandleUpdateService(ILogger<HandleUpdateService> logger, ITelegramBotClient botClient)
        {
            _logger = logger;
            _botClient = botClient;
        }

        public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            var handler = update switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
                { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
                { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
                { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
                { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update, cancellationToken)
            };

            await handler;
        }

        private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Receive message type: {MessageType}", message.Type);
            if (message.Text is not { } messageText)
                return;

            var action = messageText.Split(' ')[0] switch
            {
                "/inline_keyboard" => SendInlineKeyboard(_botClient, message, cancellationToken),
                "/keyboard" => SendReplyKeyboard(_botClient, message, cancellationToken),
                "/remove" => RemoveKeyboard(_botClient, message, cancellationToken),
                "/photo" => SendFile(_botClient, message, cancellationToken),
                "/request" => RequestContactAndLocation(_botClient, message, cancellationToken),
                "/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
                _ => Usage(_botClient, message, cancellationToken)
            };
            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                await botClient.SendChatActionAsync(
                    chatId: message.Chat.Id,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);

                // Simulate longer running task
                await Task.Delay(500, cancellationToken);

                InlineKeyboardMarkup inlineKeyboard = new(
                    new[]
                    {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    },
                    });

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new(
                    new[]
                    {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                    })
                {
                    ResizeKeyboard = true
                };

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Removing keyboard",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                await botClient.SendChatActionAsync(
                    message.Chat.Id,
                    ChatAction.UploadPhoto,
                    cancellationToken: cancellationToken);

                const string filePath = "Files/tux.png";
                await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

                return await botClient.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputFileStream(fileStream, fileName),
                    caption: "Nice Picture",
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                ReplyKeyboardMarkup RequestReplyKeyboard = new(
                    new[]
                    {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                    });

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Who or Where are you?",
                    replyMarkup: RequestReplyKeyboard,
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                const string usage = "Usage:\n" +
                                     "/inline_keyboard - send inline keyboard\n" +
                                     "/keyboard    - send custom keyboard\n" +
                                     "/remove      - remove custom keyboard\n" +
                                     "/photo       - send a photo\n" +
                                     "/request     - request location or contact\n" +
                                     "/inline_mode - send keyboard with Inline Query";

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                InlineKeyboardMarkup inlineKeyboard = new(
                    InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Press the button to start Inline Query",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken);
            }
        }
        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}",
                cancellationToken: cancellationToken);

            await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                text: $"Received {callbackQuery.Data}",
                cancellationToken: cancellationToken);
        }

        private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

            InlineQueryResult[] results = {
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

            await _botClient.AnswerInlineQueryAsync(
                inlineQueryId: inlineQuery.Id,
                results: results,
                cacheTime: 0,
                isPersonal: true,
                cancellationToken: cancellationToken);
        }

        private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

            await _botClient.SendTextMessageAsync(
                chatId: chosenInlineResult.From.Id,
                text: $"You chose result with Id: {chosenInlineResult.ResultId}",
                cancellationToken: cancellationToken);
        }

        private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)

        {
            _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
    }
}

//        public async Task EchoAsync(Update update)
//        {
//            var handler = update.Type switch
//            {
//                UpdateType.Message => BotOnMessageRecieved(update.Message),
//                UpdateType.CallbackQuery => BotOnCallbackQueryRecieved(update.CallbackQuery),
//                _ => UnknownUpdateTypeHandler(update)
//            };

//            try
//            {
//                await handler;
//            }
//            catch (Exception ex)
//            {
//                await HandlerErrorAsync(ex);
//            }
//        }

//        private Task UnknownUpdateTypeHandler(Update update)
//        {
//            _logger.LogInformation($"Unknown update type : {update.Type}");
//            return Task.CompletedTask;
//        }

//        public Task HandlerErrorAsync(Exception ex)
//        {

//            var ErrorMessage = ex switch
//            {
//                ApiRequestException apiRequestException => $"Telegram Api Error:\n{apiRequestException.ErrorCode}",
//                _ => ex.ToString()
//            };
//            _logger.LogInformation(ErrorMessage);

//            return Task.CompletedTask;

//        }

//        private async Task BotOnCallbackQueryRecieved(CallbackQuery callbackQuery)
//        {
//            await _botClient.SendTextMessageAsync(
//                chatId: callbackQuery.Message.Chat.Id, 
//                text: $"{callbackQuery.Data}");
//        }

//        private  async Task BotOnMessageRecieved(Message message)
//        {
//            _logger.LogInformation($"Message keldi: {message.Type}");

//            await _botClient.SendTextMessageAsync(
//                chatId: message.Chat.Id,
//                text: "Botga xabar keldi");
//        }
//    }
//}
