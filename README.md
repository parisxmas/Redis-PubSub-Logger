# Redis-PubSub-Logger
Simple .Net Core Redis Logger Extension

Usage;

  public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            
            loggerFactory.AddProvider(new RedisPubSubLoggerProvider(
                                      new RedisPubSubLoggerConfiguration
                                      {                                         
                                          RedisConnectionString= "xxx.xxxx.xxx.xxx:6379",
                                          RedisLogChannel = "logger"
                                      }));           
        }
