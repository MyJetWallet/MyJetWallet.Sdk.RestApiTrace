using System;
using System.Net;

namespace MyJetWallet.Sdk.RestApiTrace
{
    public static class ApiTraceItemHelper
    {
        public static ApiTraceItem LogRestCall(this ApiTraceItem item, string mehod, string host, string path, HttpStatusCode statusCode, long? executeTimeMs, string userAgent)
        {
            item.Method = mehod;
            item.HostUrl = host;
            item.Path = path;
            item.StatusCode = ((int)statusCode).ToString();
            item.Status = statusCode.ToString();
            item.ExecuteTimeMs = executeTimeMs?.ToString();
            item.UserAgent = userAgent;

            return item;
        }

        public static ApiTraceItem ApplyException(this ApiTraceItem item, Exception exception)
        {
            if (exception != null)
            {
                item.ExceptionType = exception.GetType().Name;
                item.ExceptionStack = exception.StackTrace;
                item.ExceptionMsg = exception.Message;
            }
            return item;
        }

        public static ApiTraceItem ClientIdentity(this ApiTraceItem item, string brokerId, string brandId, string clientId)
        {
            if (!string.IsNullOrEmpty(brokerId)) item.BrokerId = brokerId;
            if (!string.IsNullOrEmpty(brandId)) item.BrandId = brandId;
            if (!string.IsNullOrEmpty(clientId)) item.ClientId = clientId;

            return item;
        }

        public static ApiTraceItem ClientWallet(this ApiTraceItem item, string walletId)
        {
            if (!string.IsNullOrEmpty(walletId)) item.WalletId = walletId;

            return item;
        }

        public static ApiTraceItem IP(this ApiTraceItem item, string ip, string country)
        {
            item.IP = ip;
            item.IPCountry = country;
            return item;
        }
    }
}