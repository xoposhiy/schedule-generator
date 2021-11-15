using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Domain.Algorithms;
using Domain.Algorithms.Solvers;
using Infrastructure;
using Infrastructure.GoogleSheetsRepository;
using Ninject;
using Ninject.Extensions.Conventions;
using static Domain.Conversions.ScheduleSpreadsheetConverter;
using static Infrastructure.SheetConstants;
using static Domain.DomainExtensions;
using static Infrastructure.LoggerConstants;

namespace ScheduleCLI
{
    public static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            //var container = ConfigureContainer();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine("Starting...");

            SheetNamesConfig[] configs =
            {
                SpringConfig
                // AutumnConfig
            };

            foreach (var config in configs) MakeAndWriteSchedule(config);
        }

        // ReSharper disable once UnusedMember.Local
        private static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            return container;
        }

        private static readonly TimeSpan[] TimeSpans =
        {
            new(0, 0, 15),
            new(0, 1, 0),
            new(0, 10, 0),
            new(8, 0, 0),
            TimeSpan.MaxValue
        };


        private static void MakeAndWriteSchedule(SheetNamesConfig config)
        {
            var solver = GetSolver(config, Repository);
            var (schedule, _) = solver.GetSolution(TimeSpans[0]);

            var notUsedMeetings = string.Join("\n", schedule.NotUsedMeetings);
            Console.WriteLine(notUsedMeetings);

            BuildSchedule(schedule, Repository, config.Schedule);
            // BuildScheduleByTeacher(schedule, Repository, "Расписание по преподу");
            // WriteRowMeetings(schedule, RowMeetingsRepository, "Расписание");
            var logger = new Logger("Combined");

            var estimator = GetDefaultCombinedEstimator();
            estimator.Estimate(schedule, logger);
            if (WriteToFiles)
            {
                using StreamWriter streamWriter = new(string.Join("\\", LoggerInfoPath, "LoggerInfo.txt"));
                streamWriter.WriteLine(logger);
            }
            else
            {
                Console.WriteLine(logger);
            }
        }

        public static ISolver GetSolver(SheetNamesConfig sheetNamesConfig, GsRepository repo)
        {
            var (requisition, classrooms) = GetRequisition(sheetNamesConfig, repo);
            var estimator = GetDefaultCombinedEstimator();

            //return new GreedySolver(estimator, requisition, classrooms, new(42));
            return new RepeaterSolver(new GreedySolver(estimator, requisition, classrooms, new(228322), 3));
            // return new BeamSolver(estimator, requisition, classrooms, /*new(42),*/ 20);
            //return new RepeaterSolver(new BeamSolver(estimator, requisition, classrooms, new(42), 5));
        }
    }
}