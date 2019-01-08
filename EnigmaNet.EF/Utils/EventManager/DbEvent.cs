using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

using EnigmaNetCore.Bus;

namespace EnigmaNetCore.EF.Utils.EventPublisher
{
    public class DbEvent : Event
    {
        public DbContext DbContext { get; set; }
        public Event[] Events { get; set; }

        public static DbEvent Create(DbContext dbContext, params Event[] events)
        {
            return new DbEvent
            {
                DbContext = dbContext,
                DateTime = DateTime.Now,
                EventId = Guid.NewGuid().ToString(),
                Events = events
            };
        }
    }
}
