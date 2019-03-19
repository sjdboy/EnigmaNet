using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnigmaNet.AspNet.Middlewares.GetRoutes
{
    public class GetRoutesMiddleware
    {
        class RoutingFeature : IRoutingFeature
        {
            public RouteData RouteData { get; set; }
        }

        IRouter _router;
        RequestDelegate _next;

        public GetRoutesMiddleware(RequestDelegate next, IRouter router)
        {
            _next = next;
            _router = router;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new RouteContext(httpContext);

            context.RouteData.Routers.Add(_router);

            await _router.RouteAsync(context);

            if (context.Handler != null)
            {
                httpContext.Features[typeof(IRoutingFeature)] = new RoutingFeature
                {
                    RouteData = context.RouteData
                };
            }

            await _next(httpContext);
        }
    }
}
