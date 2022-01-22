using System;
using System.Globalization;
using System.Text;
using System.Threading;
using Domain.Algorithms;
using Domain.Algorithms.Solvers;
using Domain.Conversions;
using Infrastructure;
using Infrastructure.GoogleSheetsRepository;
using static Infrastructure.SheetConstants;
using static Domain.DomainExtensions;
using static Infrastructure.LoggerExtension;

namespace ScheduleCLI
{
    public static class Program
    {
        private static readonly TimeSpan[] TimeSpans =
        {
            new(0, 0, 15),
            new(0, 1, 0),
            new(0, 10, 0),
            new(1, 0, 0),
            new(8, 0, 0),
            TimeSpan.MaxValue
        };

        private static readonly TimeSpan TimeLimit = TimeSpans[1];
        private const int BeamWidth = 50;
        private const int ChoiceCount = 3;

        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            WriteLog("Starting...");

            SheetNamesConfig[] configs =
            {
                // SpringConfig
                AutumnConfig
            };
            WriteLog($"{configs.Length} configs");

            foreach (var config in configs) MakeAndWriteSchedule(config);
        }

        private static void MakeAndWriteSchedule(SheetNamesConfig config)
        {
            WriteLog($"With time limit of {TimeLimit}");
            var solver = GetSolver(config, Repository);
            var (schedule, _) = solver.GetSolution(TimeLimit);

            var notUsedMeetings = string.Join("\n", schedule.NotUsedMeetings);
            WriteLog(notUsedMeetings);
            

            ScheduleSpreadsheetConverter.BuildSchedule(schedule, Repository, config.Schedule);
            // ScheduleSpreadsheetConverter.BuildScheduleByTeacher(schedule, Repository, "Расписание по преподу");
            // ScheduleSpreadsheetConverter.WriteRowMeetings(schedule, RowMeetingsRepository, "Расписание");
            
            using var logger = new Logger("Combined");
            var combinedEstimator = GetDefaultCombinedEstimator();
            combinedEstimator.Estimate(schedule, logger);

            using var justiceLogger = new Logger("Justice");
            var justiceEstimator = GetDefaultJusticeEstimator();
            justiceEstimator.Estimate(schedule, justiceLogger);
        }

        private static ISolver GetSolver(SheetNamesConfig sheetNamesConfig, GsRepository repo)
        {
            // return GetRepeaterSolver(sheetNamesConfig, repo);
            return GetBeamSolver(sheetNamesConfig, repo, BeamWidth);
        }

        public static ISolver GetRepeaterSolver(SheetNamesConfig sheetNamesConfig, GsRepository repo)
        {
            var random = new ThreadSafeRandom();
            var (requisition, classrooms) = GetRequisition(sheetNamesConfig, repo);
            var estimator = GetDefaultCombinedEstimator();
            var greedy = new GreedySolver(estimator, requisition, classrooms, random, ChoiceCount);
            return new RepeaterSolver(greedy);
        }

        public static ISolver GetBeamSolver(SheetNamesConfig sheetNamesConfig, GsRepository repo, int beamWidth = 5)
        {
            var random = new ThreadSafeRandom();
            var (requisition, classrooms) = GetRequisition(sheetNamesConfig, repo);
            var estimator = GetDefaultCombinedEstimator();
            var greedy = new GreedySolver(estimator, requisition, classrooms, random);
            return new BeamSolver(estimator, requisition, classrooms, greedy, beamWidth);
        }
    }
}