using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScheduleLib
{
    class Rule
    {
        public int Weight;
        public Func<Schedule, IEnumerable<Meeting>> GetConflictingMeetings;

        public Rule(Func<Schedule, IEnumerable<Meeting>> getConflictingMeetings, int weight)
        {
            GetConflictingMeetings = getConflictingMeetings;
            Weight = weight;
        }
    }
}
