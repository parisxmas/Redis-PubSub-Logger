using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Redis.PubSub.Logger
{
    public class RedisPubSubLogger : ILogger
    {
        private readonly string _name;
        private readonly RedisPubSubLoggerConfiguration _config;
        private readonly RedisConnection redisConnection;
     
        public RedisPubSubLogger(string name, RedisPubSubLoggerConfiguration config)
        {
            _name = name;
            _config = config;
            try
            {
                redisConnection = RedisConnection.GetRedisConnection(config);
            } catch (Exception ex)
            {
                throw ex;
            }
            #if DEBUG
                // you can see here how many loggers created in debug mode
                System.Diagnostics.Debug.WriteLine($"Redis logger subscriber {name}");
            #endif
        }

        private ConnectionMultiplexer GetConnection()
        {
            return ConnectionMultiplexer.Connect(_config.RedisConnectionString);
        }


        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {            
            // all levels enabled
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel) && _config.RedisLogChannel == null && redisConnection.sub == null)
            {
                return;
            }            
            var message = new LogModel()
            {
                EventId = eventId.Id,
                LogLevel = logLevel,
                LogLevelName = logLevel.ToString(),
                Exception = exception,
                Name = _name,
                State = formatter(state,exception)
            };
            
            // catch it here as you want
            redisConnection.sub.PublishAsync(_config.RedisLogChannel, JsonConvert.SerializeObject(message));
        }
    }

    public class RedisPubSubLoggerConfiguration
    {  
        public int EventId { get; set; } = 0;
        public string RedisConnectionString;
        public string RedisLogChannel;
    }

    public class LogModel
    {
        public LogLevel LogLevel { get; set; }
        public string LogLevelName { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; }
        public object State { get; set; }
        public Exception Exception { get; set; } 
    }

    public class RedisPubSubLoggerProvider : ILoggerProvider
    {
        private readonly RedisPubSubLoggerConfiguration _config;
        private readonly ConcurrentDictionary<string, RedisPubSubLogger> _loggers = new ConcurrentDictionary<string, RedisPubSubLogger>();

        public RedisPubSubLoggerProvider(RedisPubSubLoggerConfiguration config)
        {
            _config = config;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new RedisPubSubLogger(name, _config));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public sealed class RedisConnection
    {
        private static RedisConnection instance;
        private static ConnectionMultiplexer connectionMultiplexer;
        public ISubscriber sub;
        public RedisConnection(RedisPubSubLoggerConfiguration config)
        {
            connectionMultiplexer = ConnectionMultiplexer.Connect(config.RedisConnectionString);
            sub = connectionMultiplexer.GetSubscriber();
        }
        public static RedisConnection GetRedisConnection(RedisPubSubLoggerConfiguration config)
        {
            if (instance == null)
            {
                instance = new RedisConnection(config);
            }
            return instance;
        }
    }
}
