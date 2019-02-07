using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using EnigmaNet.Bus;
using EnigmaNet.Components;
using Microsoft.EntityFrameworkCore;

namespace EnigmaNet.EF.Utils.EventManager
{
    public class DbEventManager// : IEventPublisher
    {
        #region private

        const int BatchAmount = 100;
        const int StartWaitMillisSecnods = 1000 * 5;
        const int WaitMillisSeconds = 1000 * 3;
        const int SecondWaitMillisSeconds = 1000 * 2;
        const int FailEventWaitSeconds = 1000 * 60 * 1;
        const int CommonErrorTimes = 3;
        ILogger _logger;
        AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LoggerFactory.CreateLogger<DbEventManager>();
                }
                return _logger;
            }
        }

        void SendEvents(int errorTime, bool isMoreThan)
        {
            Logger.LogInformation($"do SendEvents,isMoreThan:{isMoreThan} errorTime:{errorTime}");
            using (var dbContext = DbContextFactory.Create())
            {
                while (true)
                {
                    var query = dbContext.Set<EventRecord>().OrderBy(m => m.Id).Where(m => m.Processed == false);

                    if (isMoreThan)
                    {
                        query = query.Where(m => m.ErrorTimes > errorTime);
                    }
                    else
                    {
                        query = query.Where(m => m.ErrorTimes <= errorTime);
                    }

                    var records = query.OrderBy(m => m.ErrorTimes).Take(BatchAmount).ToList();

                    Logger.LogInformation($"get reocrds,count:{records?.Count}");

                    if (!(records.Count > 0))
                    {
                        break;
                    }

                    foreach (var record in records)
                    {
                        bool sendOk;
                        try
                        {
                            var @event = (Event)Newtonsoft.Json.JsonConvert.DeserializeObject(record.EventObjectJson, new Newtonsoft.Json.JsonSerializerSettings
                            {
                                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                            });

                            Logger.LogInformation($"deserialize event,record:{record.Id} type:{@event.GetType()} data:{Newtonsoft.Json.JsonConvert.SerializeObject(@event)}");

                            EventPublisher.PublishAsync(@event).Wait();

                            Logger.LogInformation($"send event success");

                            sendOk = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"send event fail,eventRecordId:{record.Id}");
                            sendOk = false;
                        }

                        if (sendOk)
                        {
                            record.Processed = true;
                        }
                        else
                        {
                            record.ErrorTimes += 1;
                        }
                        record.ProcessDateTime = DateTime.Now;

                        dbContext.SaveChanges();
                    }
                }
            }
        }

        void StartSendCommonEventTask()
        {
            var thread = new Thread(new ThreadStart(SendCommonEventTask));
            thread.IsBackground = false;
            thread.Start();
        }

        void SendCommonEventTask()
        {
            Logger.LogInformation($"start SendCommonEventTask");

            Thread.CurrentThread.Join(StartWaitMillisSecnods);

            while (true)
            {
                if (_autoResetEvent.WaitOne(WaitMillisSeconds))
                {
                    Logger.LogDebug($"wait receive signal");
                }
                else
                {
                    Logger.LogDebug($"wait time out");
                }

                Thread.CurrentThread.Join(SecondWaitMillisSeconds);

                try
                {
                    SendEvents(CommonErrorTimes, false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
            }
        }

        void StartSendFailEventTask()
        {
            var thread = new Thread(new ThreadStart(SendFailEventTask));
            thread.IsBackground = false;
            thread.Start();
        }

        void SendFailEventTask()
        {
            Logger.LogInformation($"start SendFailEventTask");
            while (true)
            {
                Thread.CurrentThread.Join(FailEventWaitSeconds);

                try
                {
                    SendEvents(CommonErrorTimes, true);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
            }
        }

        #endregion

        public IEventPublisher EventPublisher { get; set; }

        public ILongIdProduced EventRecordIdProducer { get; set; }

        public ILongIdProduced ConsumeEventRecordIdProducer { get; set; }

        public IDbContextFactory DbContextFactory { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        public async Task AppendEventAsync(DbContext dbContext, params Event[] events)
        {
            if ((events?.Length ?? 0) == 0)
            {
                return;
            }

            foreach (var @event in events)
            {
                var eventJson = Newtonsoft.Json.JsonConvert.SerializeObject(@event, new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                });

                dbContext.Add(new EventRecord
                {
                    Id = await EventRecordIdProducer.GenerateIdAsync(),
                    EventObjectJson = eventJson,
                    DateTime = DateTime.Now,
                });
            }

            _autoResetEvent.Set();
        }

        public async Task<bool> CheckConsumedAsync(DbContext dbContext, string eventId, string eventConsumerId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentNullException(nameof(eventId));
            }
            if (string.IsNullOrEmpty(eventConsumerId))
            {
                throw new ArgumentNullException(nameof(eventConsumerId));
            }
            return await dbContext.Set<ConsumeEventRecord>().AnyAsync(m => m.EventId == eventId && m.ConsumerId == eventConsumerId);
        }

        public async Task RecordConsumeAsync(DbContext dbContext, string eventId, string eventConsumerId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentNullException(nameof(eventId));
            }
            if (string.IsNullOrEmpty(eventConsumerId))
            {
                throw new ArgumentNullException(nameof(eventConsumerId));
            }

            dbContext.Add(new ConsumeEventRecord
            {
                Id = await ConsumeEventRecordIdProducer.GenerateIdAsync(),
                EventId = eventId,
                ConsumerId = eventConsumerId,
                DateTime = DateTime.Now,
            });
        }

        public void StartEventHanlderTask()
        {
            StartSendCommonEventTask();
            StartSendFailEventTask();
        }
    }
}
