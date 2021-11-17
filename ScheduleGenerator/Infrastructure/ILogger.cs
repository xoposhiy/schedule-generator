using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Infrastructure
{
    public interface ILogger : IDisposable
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

        private readonly Logger? parent;
        private readonly int level;

        public Logger(string name, double weight = 1, int topN = 100)
        {
            this.name = name;
            this.weight = weight;
            this.topN = topN;
            parent = null;
        }

        private Logger(string name, double weight, int topN, Logger parent) : this(name, weight, topN)
        {
            this.parent = parent;
            level = parent.level + 1;
        }

        public void Log(string message, double score)
        {
            if (score == 0) return;
            records.Add(new(message, score));
            totalScore += score;
        }

        public ILogger GetChild(string childName, double childWeight = 1, int childTopN = 10)
        {
            var child = new Logger(childName, childWeight, childTopN, this);
            return child;
        }

        public override string ToString()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var offset = string.Join("", Enumerable.Repeat(Tab, level));
            var lines = records
                .OrderBy(r => r.Score)
                .Take(topN)
                .Select(m => offset + m.Message)
                .Append($"{offset}Total: {totalScore * weight} (BasicScore: {totalScore}, Weight: {weight})")
                .Prepend($"{name}:");

            return string.Join(Environment.NewLine, lines);
        }

        public void Dispose()
        {
            var message = ToString();
            if (parent == null)
                LoggerExtension.WriteLog(message);
            else
                parent.Log(message, totalScore * weight);
        }

        ~Logger()
        {
            Dispose();
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