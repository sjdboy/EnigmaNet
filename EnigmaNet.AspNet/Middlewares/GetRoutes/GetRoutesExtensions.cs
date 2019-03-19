using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EnigmaNet.AspNet.Middlewares.GetRoutes
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// https://stackoverflow.com/questions/38505184/asp-net-core-get-routedata-value-from-url
    /// </remarks>
    public static class GetRoutesExtensions
    {
        public static IApplicationBuilder UseGetRoutes(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var routes = new RouteBuilder(app)
            {
                DefaultHandler = app.ApplicationServices.GetRequiredService<MvcRouteHandler>(),
            };

            configureRoutes(routes);

            routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(app.ApplicationServices));

            var router = routes.Build();

            return app.UseMiddleware<GetRoutesMiddleware>(router);
        }
    }
}
