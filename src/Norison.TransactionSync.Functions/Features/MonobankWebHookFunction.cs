using System.Text.Json;

using Mediator;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;

using Monobank.Client;

using Norison.TransactionSync.Application.Features.Commands.ProcessMonoWebHookData;

namespace Norison.TransactionSync.Functions.Features;

public class MonobankWebHookFunction(ISender sender, IMemoryCache memoryCache)
{
    [Function(nameof(MonobankWebHookFunction))]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "monobank/{chatId}")]
        HttpRequest req)
    {
        if (req.Method == "GET")
        {
            return new OkResult();
        }

        using var reader = new StreamReader(req.Body);
        var content = await reader.ReadToEndAsync();
        var chatId = long.Parse(req.RouteValues["chatId"]!.ToString()!);

        _ = ProcessWebHookData(chatId, content);

        return new OkResult();
    }

    private async Task ProcessWebHookData(long chatId, string content)
    {
        var webHookModel = JsonSerializer.Deserialize<WebHookModel>(content);

        if (webHookModel is null || memoryCache.TryGetValue(webHookModel.Data.StatementItem.Id, out _))
        {
            return;
        }

        var data = webHookModel.Data;
        var transactionId = data.StatementItem.Id;

        memoryCache.Set(transactionId, transactionId, TimeSpan.FromMinutes(10));

        var command = new ProcessMonoWebHookDataCommand { ChatId = chatId, WebHookData = data };
        await sender.Send(command);
    }
}