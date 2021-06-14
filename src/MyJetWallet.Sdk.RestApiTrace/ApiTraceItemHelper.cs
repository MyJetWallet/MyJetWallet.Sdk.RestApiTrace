using System;
using System.Net;

namespace MyJetWallet.Sdk.RestApiTrace
{
    public static class ApiTraceItemHelper
    {
        public static ApiTraceItem LogRestCall(this ApiTraceItem item, string mehod, string host, string path, HttpStatusCode statusCode, long? executeTimeMs, string userAgent)
        {
            item["Component"] = host;
            item["Process"] = path;

            item["Method"] = mehod;
            item["Host"] = host;
            item["Path"] = path;
            item["StatusCode"] = (int)statusCode;
            item["Status"] = statusCode.ToString();
            item["ExecuteTimeMs"] = executeTimeMs;
            item["UserAgent"] = userAgent;

            return item;
        }

        public static ApiTraceItem ApplyException(this ApiTraceItem item, Exception exception)
        {
            if (exception != null)
            {
                item["Type"] = exception.GetType().Name;
                item["Stack"] = exception.StackTrace;
                item["Msg"] = exception.ToString();
            }
            return item;
        }

        public static ApiTraceItem ClientIdentity(this ApiTraceItem item, string brokerId, string brandId, string clientId)
        {
            if (!string.IsNullOrEmpty(brokerId)) item["BrokerId"] = brokerId;
            if (!string.IsNullOrEmpty(brandId)) item["BrandId"] = brandId;
            if (!string.IsNullOrEmpty(clientId)) item["ClientId"] = clientId;

            return item;
        }

        public static ApiTraceItem ClientWallet(this ApiTraceItem item, string walletId)
        {
            if (!string.IsNullOrEmpty(walletId)) item["WalletId"] = walletId;

            return item;
        }

        public static ApiTraceItem IP(this ApiTraceItem item, string ip)
        {
            item["IP"] = ip;
            return item;
        }

        public static ApiTraceItem AddField(this ApiTraceItem item, string name, string value)
        {
            item[name] = value;
            return item;
        }
    }
}