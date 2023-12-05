using Telegram.Bot;
using Telegrambot.Controllers;
using Telegrambot.Models;
using Telegrambot.Services;

var builder = WebApplication.CreateBuilder(args);

var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.Configuration);
builder.Services.Configure<BotConfiguration>(botConfigurationSection);

var botConfiguration = botConfigurationSection.Get<BotConfiguration>();

builder.Services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

builder.Services.AddScoped<HandleUpdateService>();

builder.Services.AddHostedService<ConfigureWebhook>();


builder.Services
    .AddControllers()
    .AddNewtonsoftJson();

var app = builder.Build();

#pragma warning disable CS8602 // –азыменование веро€тной пустой ссылки.
app.MapBotWebhookRoute<WebhookController>(route: botConfiguration.Route);
#pragma warning restore CS8602 // –азыменование веро€тной пустой ссылки.
app.MapControllers();
app.Run();


