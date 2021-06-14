using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;
using Nest;
using Newtonsoft.Json;

namespace MyJetWallet.Sdk.RestApiTrace
{
    public class ApiTraceManager : IApiTraceManager, IDisposable, IStartable
    {
        public static int MaxCountInCache { get; set; } = 1000;

        private readonly LogElkSettings _elkSettings;
        private readonly string _elkIndexPrefix;
        private readonly ILogger _logger;
        private readonly MyTaskTimer _timer;

        private List<ApiTraceItem> _data = new(MaxCountInCache);
        private readonly object _gate = new();
        private ElasticClient _client;

        public ApiTraceManager(LogElkSettings elkSettings, string elkIndexPrefix, ILogger logger)
        {
            _elkSettings = elkSettings;
            _elkIndexPrefix = elkIndexPrefix;
            _logger = logger;

            _timer = new MyTaskTimer(nameof(ApiTraceManager), TimeSpan.FromSeconds(1), _logger, HandleTraces);


        }

        private DateTime _current = DateTime.MinValue;
        private string _index = "";

        private string IndexName()
        {
            if (DateTime.UtcNow.Date == _current)
                return _index;

            _current = DateTime.UtcNow.Date;
            _index = $"{_elkIndexPrefix}-{_current:yyyy-MM-dd}";
            return _index;
        }

        private async Task HandleTraces()
        {
            List<ApiTraceItem> data;
            lock (_gate)
            {
                if (!_data.Any())
                    return;

                data = _data;
                _data = new List<ApiTraceItem>(MaxCountInCache);
            }

            using (var activity = MyTelemetry.StartActivity("Write trace to ELK"))
            {
                try
                {
                    activity.AddTag("count", data.Count);

                    var resp = await _client.IndexManyAsync(data);

                    resp.IsValid.AddToActivityAsTag("is-valid");
                    resp.Errors.AddToActivityAsTag("errors");

                    if (resp.Errors)
                        resp.ItemsWithErrors.Count().AddToActivityAsTag("count-with-errors");

                    Console.WriteLine($"Cannot Send trace to ELK: {resp.IsValid} {resp.Errors}");
                }
                catch (Exception ex)
                {
                    ex.FailActivity();
                    Console.WriteLine($"Cannot Send trace to ELK:\n{ex.ToString()}");
                }
            }


            foreach (var item in data)
            {
                Console.WriteLine($"api trace:\n{JsonConvert.SerializeObject(item, Formatting.Indented)}");
            }
        }

        public void LogMethodCall(ApiTraceItem item)
        {
            lock (_gate)
            {
                if (_data.Count < MaxCountInCache)
                    _data.Add(item);
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        public void Start()
        {
            var uris = _elkSettings.Urls.Select(e => new Uri(e.Value)).ToArray();


            var connectionPool = new SniffingConnectionPool(uris);
            var settings = new ConnectionSettings(connectionPool)
                .BasicAuthentication(_elkSettings.User, _elkSettings.Password)
                .DefaultIndex(_elkIndexPrefix);

            //var settings = new ConnectionSettings(uris.First())
            //    .BasicAuthentication(_elkSettings.User, _elkSettings.Password)
            //    .DefaultIndex(_elkIndexPrefix);

            _client = new ElasticClient(settings);

            _timer.Start();
        }
    }
}