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

            var config = SpringConfig;

            // TODO все Estimators: нормализовать score во всех estimator-ах, чтобы масштаб чисел на выходе был схожий.

            var solver = GetSolver(config, Repository);
            var solutions = solver.GetSolution(new(0, 1, 5)).ToList();

            var converter = new ScheduleSpreadsheetConverter(Repository, config.Schedule);
            converter.Build(solutions.Last().Schedule);
            var logger = new Logger("Combined");

            var estimator = GetDefaultCombinedEstimator();
            estimator.Estimate(solutions.Last().Schedule, logger);
            Console.WriteLine(logger);
        }

        // ReSharper disable once UnusedMember.Local
        private static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            return container;
        }
    }
}