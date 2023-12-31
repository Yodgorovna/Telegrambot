﻿using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegrambot.Models;

namespace Telegrambot.Services
{
    public class ConfigureWebhook : IHostedService
    {
        private readonly ILogger<ConfigureWebhook> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly BotConfiguration _botConfig;

        public ConfigureWebhook(
            ILogger<ConfigureWebhook> logger,
            IServiceProvider serviceProvider,
            IOptions<BotConfiguration> botOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _botConfig = botOptions.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
            var webhookAddress = $"{_botConfig.HostAddress}{_botConfig.Route}";
            _logger.LogInformation("Setting webhook: {WebhookAddress}", webhookAddress);
            await botClient.SetWebhookAsync(
                url: webhookAddress,
                allowedUpdates: Array.Empty<UpdateType>(),
                secretToken: _botConfig.SecretToken,
                cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            // Remove webhook on app shutdown
            _logger.LogInformation("Removing webhook");

            await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
        }
    }
}
//        private readonly ILogger<ConfigureWebhook> _logger;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly BotConfiguration _botConfig;

//        public ConfigureWebhook(ILogger<ConfigureWebhook> logger,
//            IServiceProvider serviceProvider,
//            IConfiguration configuration)
//        {
//            _logger = logger;
//            _serviceProvider = serviceProvider;
//            _botConfig = configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
//        }


//        public async Task StartAsync(CancellationToken cancellationToken)
//        {
//            using var scope = _serviceProvider.CreateScope();

//            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

//            var webhookAddress = $@"{_botConfig.HostAddress}/bot/{_botConfig.Token}";    

//            _logger.LogInformation("Setting webhook");

//            await botClient.SendTextMessageAsync(
//                chatId: 2117405852,
//                text: "Salom");

//            await botClient.SetWebhookAsync(
//                url: webhookAddress,
//                allowedUpdates: Array.Empty<UpdateType>(),
//                cancellationToken: cancellationToken);
//        }

//        public async Task StopAsync(CancellationToken cancellationToken)
//        {
//            using var scope = _serviceProvider.CreateScope();

//            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

//            _logger.LogInformation("Webhook removing");

//            await botClient.SendTextMessageAsync(
//                chatId: 2117405852,
//                text: "Bot uxlamoqda") ;
//        }
//    }
//}

