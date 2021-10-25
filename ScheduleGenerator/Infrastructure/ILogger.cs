using System.Collections.Generic;
using System.Linq;

namespace Infrastructure
{
    public interface ILogger
    {
        public void Log(string message, double score);

        public ILogger GetChild(string childName = "Child", int childTopN = 10);
    }

    public record LogRecord(string Message, double Score);

    public class Logger : ILogger
    {
        private readonly int topN;
        private readonly string name;
        private const string Tab = "\t";

        private double totalScore;
        private readonly List<LogRecord> records = new();
        private readonly List<Logger> children = new();

        public Logger(string name, int topN = 100)
        {
            this.name = name;
            this.topN = topN;
        }

        public void Log(string message, double score)
        {
            records.Add(new(message, score));
            totalScore += score;
        }

        public ILogger GetChild(string childName, int childTopN = 10)
        {
            var child = new Logger(childName, childTopN);
            children.Add(child);
            return child;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        private string ToString(int level)
        {
            var offset = string.Join("", Enumerable.Repeat(Tab, level));
            var score = totalScore;
            var lines = records
                .OrderBy(r => r.Score)
                .Take(topN)
                .Select(m => offset + m.Message)
                .ToList();
            foreach (var child in children.Where(c => c.totalScore != 0))
            {
                lines.Add($"{offset}{child.name}:");
                lines.Add(child.ToString(level + 1));
                score += child.totalScore;
            }

            lines.Add($"{offset}Total: {score}");
            return string.Join("\n", lines);
        }
    }
}