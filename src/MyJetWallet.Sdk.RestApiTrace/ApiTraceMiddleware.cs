using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MyJetWallet.Sdk.Authorization;
using SimpleTrading.ClientApi.Utils;

namespace MyJetWallet.Sdk.RestApiTrace
{
    public class ApiTraceMiddleware
    {
        public static Action<HttpContext, ApiTraceItem> ContextHandlerCallback { get; set; }

        private readonly RequestDelegate _next;
        private readonly IApiTraceManager _apiTraceManager;

        public ApiTraceMiddleware(RequestDelegate next, IApiTraceManager apiTraceManager)
        {
            _next = next;
            _apiTraceManager = apiTraceManager;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = new Stopwatch();
            Exception ex = null;
            sw.Start();
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                ex = e;
                throw;
            }
            finally
            {
                sw.Stop();

                var request = context.Request;
                var responce = context.Response;

                

                var item = ApiTraceItem.Create()
                    .LogRestCall(
                        request.Method,
                        request.Host.ToString(),
                        request.Path,
                        ex == null ? (HttpStatusCode)responce.StatusCode : HttpStatusCode.InternalServerError,
                        sw.ElapsedMilliseconds,
                        context.GetUserAgent())
                    .ApplyException(ex)
                    .IP(context.GetIp());

                ParseActivityAndClient(context, item);

                ContextHandlerCallback?.Invoke(context, item);

                _apiTraceManager.LogMethodCall(item);
            }
        }

        private void ParseActivityAndClient(HttpContext context, ApiTraceItem item)
        {
            var claims = context.User?.Claims.ToList() ?? new List<Claim>();
            
            var brokerId = claims.FirstOrDefault(e => e.Type == AuthorizationConst.BrokerIdClaim);
            var clientId = claims.FirstOrDefault(e => e.Type == AuthorizationConst.ClientIdClaim);
            var brandId = claims.FirstOrDefault(e => e.Type == AuthorizationConst.BrandIdClaim);
            var sessionId = claims.FirstOrDefault(e => e.Type == AuthorizationConst.SessionRootIdClaim);
            var tokenId = claims.FirstOrDefault(e => e.Type == AuthorizationConst.SessionTokenIdClaim);

            if (brokerId != null) item[AuthorizationConst.BrokerIdClaim] = brokerId.Value;
            if (clientId != null) item[AuthorizationConst.ClientIdClaim] = clientId.Value;
            if (brandId != null) item[AuthorizationConst.BrandIdClaim] = brandId.Value;
            if (sessionId != null) item[AuthorizationConst.SessionRootIdClaim] = sessionId.Value;
            if (tokenId != null) item[AuthorizationConst.SessionTokenIdClaim] = tokenId.Value;

            var activity = Activity.Current;
            if (activity != null)
            {
                item["Span_Id"] = activity.SpanId.ToString();
                item["Trace_Id"] = activity.TraceId.ToString();
            }
        }
    }
}