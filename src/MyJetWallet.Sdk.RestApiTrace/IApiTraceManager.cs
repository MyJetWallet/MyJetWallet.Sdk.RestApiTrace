namespace MyJetWallet.Sdk.RestApiTrace
{
    public interface IApiTraceManager
    {
        void LogMethodCall(ApiTraceItem item);
    }
}