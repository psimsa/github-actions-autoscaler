# Issue: Missing Rate Limiting

## Current Problem
There's no rate limiting on API endpoints.

## Recommendation
Implement rate limiting to prevent abuse.

## Implementation Steps

1. Add rate limiting package:
```bash
dotnet add package AspNetCoreRateLimit
```

2. Configure rate limiting in Program.cs:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

app.UseIpRateLimiting();

app.Run();
```

3. Add rate limiting configuration:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-Client-ID",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      }
    ]
  },
  "IpRateLimitPolicies": {
    "IpRules": [
      {
        "Ip": "192.168.0.1",
        "Rules": [
          {
            "Endpoint": "*",
            "Period": "1m",
            "Limit": 1000
          }
        ]
      }
    ]
  }
}
```

4. Create custom rate limiting rules:
```csharp
public static class RateLimitExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1m",
                    Limit = 100
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 1000
                }
            };
        });

        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }
}
```

5. Add rate limiting middleware:
```csharp
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCustomRateLimiting(this IApplicationBuilder app)
    {
        app.UseIpRateLimiting();
        return app;
    }
}
```

6. Update Program.cs to use custom rate limiting:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomRateLimiting();

var app = builder.Build();

app.UseCustomRateLimiting();

app.Run();
```

## Benefits
- Prevents API abuse
- Better resource protection
- Improved system stability
- More controlled API usage
- Protection against DDoS attacks
