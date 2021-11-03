using System;
using System.Text;
using Domain.Conversions;
using Infrastructure;
using Ninject;
using Ninject.Extensions.Conventions;
using static Infrastructure.SheetConstants;
using static Domain.Extensions;

namespace ScheduleCLI
{
    public static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            //var container = ConfigureContainer();

            Console.WriteLine("Starting...");

            SheetNamesConfig[] configs =
            {
                SpringConfig,
                //AutumnConfig
            };

            foreach (var config in configs) MakeAndWriteSchedule(config);

            // TODO все Estimators: нормализовать score во всех estimator-ах, чтобы масштаб чисел на выходе был схожий.
        }

        // ReSharper disable once UnusedMember.Local
        private static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            return container;
        }

        public static void MakeAndWriteSchedule(SheetNamesConfig config)
        {
            var solver = GetSolver(config, Repository);
            var (schedule, _) = solver.GetSolution(new(0, 1, 0));

            var notUsedMeetings = string.Join("\n", schedule.NotUsedMeetings);
            Console.WriteLine(notUsedMeetings);

            ScheduleSpreadsheetConverter.Build(schedule, Repository, config.Schedule);
            var logger = new Logger("Combined");

            var estimator = GetDefaultCombinedEstimator();
            estimator.Estimate(schedule, logger);
            Console.WriteLine(logger);
        }
    }
}