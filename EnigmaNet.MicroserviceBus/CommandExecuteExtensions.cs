using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.Extensions.Configuration;

namespace EnigmaNet.MicroserviceBus
{
    public static class CommandExecuteExtensions
    {
        public const string AuthenticationOptionsKey = "AuthenticationOptions";

        public static IApplicationBuilder UseCommandExecuteHandler(this IApplicationBuilder app, params string[] paths)
        {
            if (!(paths?.Length > 0))
            {
                paths = new string[] { "/command/", "/api/command" };
            }

            return app.MapWhen(context =>
            {
                return paths.Any(path =>
                {
                    return context.Request.Path.Value?.StartsWith(path, StringComparison.OrdinalIgnoreCase) == true;
                });
            }, x => { x.UseMiddleware<Middlewares.CommandExecuteHandler>(); });
        }

        public static AuthenticationBuilder AddCommandAuthentication(this AuthenticationBuilder builder, IConfiguration configuration)
        {
            var authenticationOptions = configuration.GetSection(AuthenticationOptionsKey).Get<Options.AuthenticationOptions>();

            builder.AddOAuth2Introspection(Utils.AuthUtils.IntrospectionSchemeName, options =>
            {
                options.Authority = authenticationOptions.TokenIssuer;
                options.ClientId = authenticationOptions.ApiId;
                options.ClientSecret = authenticationOptions.ApiSecret;
                options.SkipTokensWithDots = true;
                options.DiscoveryPolicy = new IdentityModel.Client.DiscoveryPolicy
                {
                    RequireHttps = false,
                };
                if (authenticationOptions.CacheSeconds > 0)
                {
                    options.CacheDuration = TimeSpan.FromSeconds(authenticationOptions.CacheSeconds);
                }
            });

            return builder;
        }
    }
}
