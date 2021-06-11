using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;
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

        public ApiTraceManager(LogElkSettings elkSettings, string elkIndexPrefix, ILogger logger)
        {
            _elkSettings = elkSettings;
            _elkIndexPrefix = elkIndexPrefix;
            _logger = logger;

            _timer = new MyTaskTimer(nameof(ApiTraceManager), TimeSpan.FromSeconds(1), _logger, HandleTraces);
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
            _timer.Start();
        }
    }
}