using System;
using AElf.Indexing.Elasticsearch.Options;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using IM.Cache;
using IM.Common;
using IM.Commons;
using IM.EntityEventHandler.Core;
using IM.EntityEventHandler.Core.Worker;
using IM.Grains;
using IM.MongoDB;
using IM.Options;
using IM.Redis;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace IM;

[DependsOn(typeof(AbpAutofacModule),
    typeof(ImMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(ImEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpEventBusRabbitMqModule))]
public class ImEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureTokenCleanupService();
        ConfigureEsIndexCreation();
        context.Services.AddHostedService<ImHostedService>();
        ConfigureCache(configuration);
        AddProxyClient(context, configuration);
        ConfigureDistributedLocking(context, configuration);
        ConfigureGraphQl(context, configuration);
        ConfigureRedisCacheProvider(context, configuration);
        context.Services.AddSingleton(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(ImGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });

        AddMessagePushService(context, configuration);
        Configure<MessagePushOptions>(configuration.GetSection("MessagePush"));
        Configure<ChatBotBasicInfoOptions>(configuration.GetSection("ChatBotBasicInfo"));
        Configure<ChatBotConfigOptions>(configuration.GetSection("ChatBotConfig"));
        Configure<RelationOneOptions>(configuration.GetSection("RelationOne"));
        
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

    private void AddProxyClient(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddHttpClient(RelationOneConstant.ClientName, httpClient =>
        {
            httpClient.BaseAddress = new Uri(configuration["RelationOne:BaseUrl"]);
            httpClient.DefaultRequestHeaders.Add(
                RelationOneConstant.KeyName, configuration["RelationOne:ApiKey"]);
        });
    }

    private void ConfigureDistributedLocking(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer
                .Connect(configuration["Redis:Configuration"]);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }
    
    private void ConfigureRedisCacheProvider(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        var multiplexer = ConnectionMultiplexer
            .Connect(configuration["Redis:Configuration"]);
        context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        context.Services.AddSingleton<ICacheProvider,RedisCacheProvider>();
    }


    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = "IM:";
            options.GlobalCacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
            };
        });
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
        
        //backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<InitReferralRankWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<AuthTokenRefreshWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<InitChatBotUsageRankWorker>());
        
        ConfigurationProvidersHelper.DisplayConfigurationProviders(context);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(IMDomainModule)); });
    }

    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }

    private void AddMessagePushService(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var baseUrl = configuration["MessagePush:BaseUrl"];
        var appId = configuration["MessagePush:AppId"];
        if (baseUrl.IsNullOrWhiteSpace())
        {
            return;
        }

        context.Services.AddHttpClient(CommonConstant.MessagePushServiceName, httpClient =>
        {
            httpClient.BaseAddress = new Uri(baseUrl);

            if (!appId.IsNullOrWhiteSpace())
            {
                httpClient.DefaultRequestHeaders.Add(
                    CommonConstant.AppIdName, appId);
            }
        });
    }
}