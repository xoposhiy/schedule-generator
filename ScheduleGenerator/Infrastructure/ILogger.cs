using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Infrastructure
{
    public interface ILogger
    {
        public void Log(string message, double score);

        public ILogger GetChild(string childName = "Child", double weight = 1, int childTopN = 10);
    }

    public record LogRecord(string Message, double Score);

    public class Logger : ILogger
    {
        private readonly int topN;
        private readonly string name;
        private readonly double weight;
        private const string Tab = "\t";

        private double totalScore;
        private readonly List<LogRecord> records = new();
        private readonly List<Logger> children = new();

        public Logger(string name, double weight = 1, int topN = 100)
        {
            this.name = name;
            this.weight = weight;
            this.topN = topN;
        }

        public void Log(string message, double score)
        {
            if (score == 0) return;
            records.Add(new(message, score));
            totalScore += score;
        }

        public ILogger GetChild(string childName, double childWeight = 1, int childTopN = 10)
        {
            var child = new Logger(childName, childWeight, childTopN);
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
                score += child.totalScore * child.weight;
            }

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            lines.Add($"{offset}Total: {score * weight} (BasicScore: {score}, Weight: {weight})");
            return string.Join(Environment.NewLine, lines);
        }
    }

    public static class LoggerExtension
    {
        private const string LoggerInfoPath = "..\\..\\..\\..\\LoggerInfo";
        private static readonly StreamWriter StreamWriter;

        static LoggerExtension()
        {
            var date = $"{DateTime.Now:s}".Replace(':', '-');
            var fileName = $"Log_{date}.txt";
            StreamWriter = new(string.Join("\\", LoggerInfoPath, fileName));
        }

        public static void WriteLog(object message)
        {
            Console.WriteLine(message);
            StreamWriter.WriteLine(message);
        }
    }
}