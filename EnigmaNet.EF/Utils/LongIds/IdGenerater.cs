using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

using EnigmaNet.Components;

namespace EnigmaNet.EF.Utils.LongIds
{
    public class IdGenerater : ILongIdProduced
    {
        #region private

        Queue<long> _idQueue = new Queue<long>();
        object _idQueueLocker = new object();

        ILogger _logger;
        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LoggerFactory.CreateLogger<IdGenerater>();
                }
                return _logger;
            }
        }

        void ApplyIdAsync(object state)
        {
            try
            {
                //id充足，则不必生成
                {
                    var idQueuCount = _idQueue.Count;
                    if (idQueuCount > ApplyThreshold)
                    {
                        if (Logger.IsEnabled(LogLevel.Debug))
                        {
                            Logger.LogDebug($"ApplyIdAsync stop,code:{Code} ApplyThreshold:{ApplyThreshold} idQueuCount:{idQueuCount}");
                        }
                        return;
                    }
                }

                long startValue;
                long endValue;
                using (var dbContext = DbContextFactory.Create())
                {
                    var entity = dbContext.Set<IdRecord>().Find(Code);

                    if (entity == null)
                    {
                        entity = new IdRecord
                        {
                            Key = Code,
                            Value = 0,
                        };
                        dbContext.Add(entity);
                    }

                    startValue = entity.Value + 1;
                    endValue = entity.Value + Batch;

                    entity.Value = endValue;
                    dbContext.SaveChanges();
                }

                lock (_idQueueLocker)
                {
                    for (var value = startValue; value <= endValue; value++)
                    {
                        _idQueue.Enqueue(value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ApplyIdAsync error");
            }
        }

        #endregion

        #region publish

        /// <summary>
        /// Id代号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 一次申请Id的步长
        /// </summary>
        public int Batch { get; set; } = 1;

        /// <summary>
        /// 申请Id的阈值
        /// </summary>
        /// <remarks>
        /// 当剩余id个数不足(含)该值时就可以启动申请id的程序
        /// </remarks>
        public int ApplyThreshold { get; set; }

        public IDbContextFactory DbContextFactory { get; set; }

        public ILoggerFactory LoggerFactory { get; set; }

        async Task<long> ILongIdProduced.GenerateIdAsync()
        {
            if (string.IsNullOrEmpty(Code))
            {
                throw new ArgumentNullException(nameof(Code));
            }

            if (Batch <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Batch));
            }

            //从数据库分配id
            bool dbRequestId = _idQueue.Count == 0;

            restart:

            if (dbRequestId)
            {
                Logger.LogDebug($"apply id from db beging,code:{Code} batch:{Batch}");

                long startValue;
                long endValue;

                //从数据库获取id范围
                using (var dbContext = DbContextFactory.Create())
                {
                    var entity = await dbContext.Set<IdRecord>().FindAsync(Code);

                    if (entity == null)
                    {
                        entity = new IdRecord
                        {
                            Key = Code,
                            Value = 0,
                        };
                        dbContext.Add(entity);
                    }

                    startValue = entity.Value + 1;
                    endValue = entity.Value + Batch;

                    entity.Value = endValue;
                    await dbContext.SaveChangesAsync();
                }

                Logger.LogDebug($"apply id from db complete,code:{Code} batch:{Batch} startValue:{startValue} endValue:{endValue}");

                //有2个及以上id，第2个起放入队列
                if (endValue > startValue)
                {
                    Logger.LogDebug($"id cache to queue,code:{Code} startValue+1:{startValue + 1} endValue:{endValue}");
                    lock (_idQueueLocker)
                    {
                        for (var value = startValue + 1; value <= endValue; value++)
                        {
                            _idQueue.Enqueue(value);
                        }
                    }
                }

                //直接返回首个id
                return startValue;
            }

            long id;
            int remainingIdCount;
            lock (_idQueueLocker)
            {
                remainingIdCount = _idQueue.Count;
                if (remainingIdCount == 0)
                {
                    dbRequestId = true;
                    goto restart;
                }
                else
                {
                    id = _idQueue.Dequeue();
                }
            }

            Logger.LogDebug($"get a id from queue,code:{Code} id:{id}");

            remainingIdCount--;
            if (ApplyThreshold > 0 && remainingIdCount <= ApplyThreshold)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug($"ApplyId,code:{Code} ApplyThreshold:{ApplyThreshold} remainingIdCount:{remainingIdCount}");
                }
                ThreadPool.QueueUserWorkItem(new WaitCallback(ApplyIdAsync));
            }

            return id;
        }

        #endregion
    }
}
