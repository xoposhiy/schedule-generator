using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScheduleLib
{
    public class EvaluationResult
    {
        public double Penalty;
        public Meeting[] BadMeetings;
        public string PenaltyDescription;
        
    }

    public interface IRule
    {
         EvaluationResult Evaluate(Schedule schedule);
    }
    
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
