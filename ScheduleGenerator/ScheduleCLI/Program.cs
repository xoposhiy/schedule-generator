﻿using System;
using System.Linq;
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
            var solution = solver.GetSolution(new(0, 1, 5));

            var converter = new ScheduleSpreadsheetConverter(Repository, config.Schedule);
            converter.Build(solution.Schedule);
            var logger = new Logger("Combined");

            var estimator = GetDefaultCombinedEstimator();
            estimator.Estimate(solution.Schedule, logger);
            Console.WriteLine(logger);
        }
    }
}