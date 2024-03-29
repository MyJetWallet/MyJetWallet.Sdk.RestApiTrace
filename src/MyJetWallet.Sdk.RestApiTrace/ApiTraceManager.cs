﻿using System;
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

        private bool _isStarted = false;

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
            if (_client == null)
            {
                var uris = _elkSettings.Urls.Select(e => new Uri(e.Value)).ToArray();


                //var connectionPool = new SniffingConnectionPool(uris);
                var settings = new ConnectionSettings(uris.First())
                    .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                    .DefaultIndex(_elkIndexPrefix);

                if (!string.IsNullOrEmpty(_elkSettings.User) && !string.IsNullOrEmpty(_elkSettings.Password))
                {
                    settings = settings.BasicAuthentication(_elkSettings.User, _elkSettings.Password);
                }

                _client = new ElasticClient(settings);
                _logger.LogInformation("ELK client for api trace is created.");
                Console.WriteLine("=== ELK client for api trace is created. ===");
            }
            
            
            List<ApiTraceItem> data;
            lock (_gate)
            {
                if (!_data.Any())
                    return;

                data = _data;
                _data = new List<ApiTraceItem>(MaxCountInCache);
            }

            using var activity = MyTelemetry.StartActivity("Write trace to ELK");
            try
            {
                activity.AddTag("count", data.Count);

                var index = IndexName();

                data.ForEach(e => e.index = index);

                var bres = await _client.BulkAsync(b => b
                    .Index(index)
                    .CreateMany(data)
                );

                if (!bres.Errors)
                {
                    foreach (var item in bres.ItemsWithErrors)
                    {
                        Console.WriteLine($"ELK item error: {item.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                ex.FailActivity();
            }
        }

        public void LogMethodCall(ApiTraceItem item)
        {
            if (!_isStarted) return;

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
            if (_elkSettings.Urls?.Any() != true || string.IsNullOrWhiteSpace(_elkSettings.Urls.First().Value))
            {
                Console.WriteLine("=== API TRACE IS DISABLE, elt node urls is empty ===");
                return;
            }
            
            _logger.LogInformation("API TRACE is started");
            Console.WriteLine("=== API TRACE is started ===");

            _timer.Start();

            _isStarted = true;
        }
    }
}