using Azure.Messaging.ServiceBus;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Monobank.Client;

using Norison.TransactionSync.Application.Options;
using Norison.TransactionSync.Application.Services.Journal;
using Norison.TransactionSync.Application.Services.Messages;
using Norison.TransactionSync.Application.Services.Users;
using Norison.TransactionSync.Functions.Options;
using Norison.TransactionSync.Persistence.Options;
using Norison.TransactionSync.Persistence.Storages;

using Telegram.Bot;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((builder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Singleton);

        services.AddMemoryCache();
        services.AddSingleton(MonobankClientFactory.Create());
        services.AddSingleton<IStorageFactory, StorageFactory>();
        services.AddSingleton<IUsersService, UsersService>();
        services.AddSingleton<IMessagesService, MessagesService>();
        services.AddSingleton<IJournalService, JournalService>();

        services.Configure<StorageFactoryOptions>(options =>
            options.NotionToken = builder.Configuration["NotionAuthToken"]!);

        services.Configure<NotionOptions>(options =>
        {
            options.NotionUsersDatabaseId = builder.Configuration["NotionUsersDatabaseId"]!;
            options.NotionJournalsDatabaseId = builder.Configuration["NotionJournalsDatabaseId"]!;
        });

        services.Configure<WebHookOptions>(options =>
            options.WebHookBaseUrl = builder.Configuration["WebHookBaseUrl"]!);

        services.Configure<QueueOptions>(options =>
            options.TransactionsQueueName = builder.Configuration["TransactionsQueueName"]!);

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(builder.Configuration["TelegramBotToken"]!));

        services.AddSingleton(new ServiceBusClient(builder.Configuration["ServiceBusConnectionString"]));
    })
    .Build();

host.Run();