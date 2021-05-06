using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

using EnigmaNet.Bus;
using Microsoft.Extensions.Logging;
using System.IO;
using Newtonsoft.Json;

namespace EnigmaNet.RabbitMQBus
{
    public class EventBufferPublisher : IEventPublisher
    {
        const int EmptyWaitTime = 10000;
        const int EmptyWaitTimeForFailQueue = 2000;
        const int ErrorWaitTimeForFailQueue = 3000;

        RabbitMQEventBus _rabbitMQEventBus;
        ConcurrentQueue<Event> _eventQueue;
        ConcurrentQueue<Event> _failQueue;
        bool _senderStarted = false;
        ILogger _logger;

        AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        void SendEvent()
        {
            _logger.LogInformation("SendEvent,start");

            while (true)
            {
                Event @event;
                if (!_eventQueue.TryDequeue(out @event))
                {
                    _logger.LogTrace("SendEvent,empty waiting");

                    var getsignal = autoResetEvent.WaitOne(EmptyWaitTime);
                    if (getsignal)
                    {
                        _logger.LogTrace("SendEvent,get new event signal");
                    }

                    continue;
                }

                try
                {
                    _rabbitMQEventBus.PublishAsync(@event).Wait();

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"SendEvent,send event ok,eventType:{@event.GetType()} eventId:{@event.EventId} eventDateTime:{@event.DateTime}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"SendEvent,send event to rabbit error");

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation($"SendEvent,enqueue event to failQueue,eventType:{@event.GetType()} eventId:{@event.EventId} eventDateTime:{@event.DateTime}");
                    }

                    _failQueue.Enqueue(@event);
                }
            }
        }

        void SendFailQueue()
        {
            _logger.LogInformation("SendFailQueue,start");

            while (true)
            {
                Event @event;
                if (!_failQueue.TryDequeue(out @event))
                {
                    Thread.CurrentThread.Join(EmptyWaitTimeForFailQueue);
                    continue;
                }

                try
                {
                    _rabbitMQEventBus.PublishAsync(@event).Wait();

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"SendFailQueue,send event ok,eventType:{@event.GetType()} eventId:{@event.EventId} eventDateTime:{@event.DateTime}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"SendFailQueue,send event to rabbit error");

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation($"SendFailQueue,enqueue event to failQueue,eventType:{@event.GetType()} eventId:{@event.EventId} eventDateTime:{@event.DateTime}");
                    }

                    _failQueue.Enqueue(@event);

                    Thread.CurrentThread.Join(ErrorWaitTimeForFailQueue);
                }
            }
        }

        void StartSenderIfNot()
        {
            if (_senderStarted)
            {
                return;
            }

            lock (this)
            {
                if (_senderStarted)
                {
                    return;
                }

                _logger.LogInformation("StartSenderIfNot,start thread");

                new Thread(new ThreadStart(SendEvent)).Start();
                new Thread(new ThreadStart(SendFailQueue)).Start();

                _senderStarted = true;
            }
        }

        public EventBufferPublisher(RabbitMQEventBus rabbitMQEventBus, ILogger<EventBufferPublisher> logger)
        {
            if (rabbitMQEventBus == null)
            {
                throw new ArgumentNullException(nameof(rabbitMQEventBus));
            }

            _rabbitMQEventBus = rabbitMQEventBus;
            _logger = logger;
            _eventQueue = new ConcurrentQueue<Event>();
            _failQueue = new ConcurrentQueue<Event>();
        }

        public Task PublishAsync<T>(T @event) where T : Event
        {
            _eventQueue.Enqueue(@event);

            StartSenderIfNot();

            autoResetEvent.Set();

            return Task.CompletedTask;
        }

        public void SendAllEvents()
        {
            _logger.LogInformation("SendAllEvents,start");

            var events = new List<Event>();

            while (_eventQueue.TryDequeue(out Event @event))
            {
                events.Add(@event);
            }

            while (_failQueue.TryDequeue(out Event @event))
            {
                events.Add(@event);
            }

            _logger.LogInformation($"SendAllEvents, get events, count:{events.Count}");

            if (!(events.Count > 0))
            {
                return;
            }

            int okAmount = 0;
            int failAmount = 0;

            var failEvents = new List<Event>();
            foreach (var @event in events)
            {
                try
                {
                    _rabbitMQEventBus.PublishAsync(@event).Wait();

                    okAmount++;

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"SendAllEvents,send event ok,eventType:{@event.GetType()} eventId:{@event.EventId} eventDateTime:{@event.DateTime}");
                    }
                }
                catch (Exception ex)
                {
                    failAmount++;

                    string eventJson;
                    try
                    {
                        eventJson = Newtonsoft.Json.JsonConvert.SerializeObject(@event);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2, $"serialize event error");
                        eventJson = null;
                    }

                    failEvents.Add(@event);

                    _logger.LogError(ex, $"SendAllEvents,send event to rabbit error,eventType:{@event.GetType()} eventId:{@event.EventId} eventDateTime:{@event.DateTime} eventJson:{eventJson}");
                }
            }

            if (failEvents.Count > 0)
            {
                var filePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "temp", "fail_events", $"{DateTime.Now.ToString("yyyyMMddHHmmss")}.json");

                var dir = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(filePath,
                    JsonConvert.SerializeObject(failEvents, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, }));

                _logger.LogInformation($"SendAllEvents,store fail events to file'{filePath}'");
            }

            _logger.LogInformation($"SendAllEvents,do finish, okAmount:{okAmount} failAmount:{failAmount}");
        }
    }
}
