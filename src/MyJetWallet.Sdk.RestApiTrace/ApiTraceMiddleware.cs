using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MyJetWallet.Sdk.Authorization;
using MyJetWallet.Sdk.Service;
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

                using var activity = MyTelemetry.StartActivity("api-trace-log");

                var request = context.Request;
                var response = context.Response;

                string path = request.Path;

                if (path.Contains("api") &&
                    !path.Contains("isalive") &&
                    !path.Contains("metrics") &&
                    !path.Contains("dependencies") &&
                    !path.Contains("dependencies"))
                {
                    if (!request.Headers.TryGetValue("cf-ipcountry", out var cnCode))
                        cnCode = string.Empty;
                    
                    var item = ApiTraceItem.Create()
                        .LogRestCall(
                            request.Method,
                            request.Host.ToString(),
                            path,
                            ex == null ? (HttpStatusCode) response.StatusCode : HttpStatusCode.InternalServerError,
                            sw.ElapsedMilliseconds,
                            context.GetUserAgent())
                        .ApplyException(ex)
                        .IP(context.GetIp(), cnCode);

                    ParseActivityAndClient(context, item);

                    ContextHandlerCallback?.Invoke(context, item);

                    _apiTraceManager.LogMethodCall(item);
                }
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

            if (brokerId != null) item.BrokerId = brokerId.Value;
            if (clientId != null) item.ClientId = clientId.Value;
            if (brandId != null) item.BrandId = brandId.Value;
            if (sessionId != null) item.SessionRootId = sessionId.Value;
            if (tokenId != null) item.SessionTokenId = tokenId.Value;

            var activity = Activity.Current;
            if (activity != null)
            {
                item.Span_Id = activity.SpanId.ToString();
                item.Trace_Id = activity.TraceId.ToString();
                var wallet = activity.GetBaggageItem("walletId");
                item.WalletId = wallet ?? "";
            }
        }
    }
}