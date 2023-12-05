using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegrambot.Filters;
using Telegrambot.Services;

namespace Telegrambot.Controllers;

public class WebhookController : ControllerBase
{
    [HttpPost]
    [ValidateTelegramBot]
    public async Task<IActionResult> Post(
    [FromBody] Update update,
        [FromServices] HandleUpdateService handleUpdateService,
        CancellationToken cancellationToken)
    {
        await handleUpdateService.HandleUpdateAsync(update, cancellationToken);
        return Ok();
    }
}
