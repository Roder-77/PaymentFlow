using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Net;

namespace PaymentFlow.Filters
{
    public class ModelValidation : IAsyncPageFilter
    {

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Path.StartsWithSegments("/admin"))
            {
                await next();
                return;
            }

            if (context.ModelState.IsValid)
            {
                await next();
                return;
            }

            var logger = context.HttpContext.RequestServices.GetService<ILogger<ModelValidation>>()!;
            var urlHelperFactory = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>()!;
            var urlHelper = urlHelperFactory.GetUrlHelper(context);
            var errors = context.ModelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage);
            var message = string.Join(", ", errors);
            var url = context.HttpContext.Request.GetDisplayUrl();

            logger.LogError($"HttpMethod: {context.HttpContext.Request.Method}, Url: {url}, Message: {message}");

            var errorUrl = urlHelper.Page("/error", new { httpStatus = (int)HttpStatusCode.BadRequest });
            context.HttpContext.Response.Redirect(errorUrl!);

            await next();
        }
    }
}
