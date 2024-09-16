// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Linq;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using CommandLine;
using Nethermind.Core.Extensions;
using BenchmarkDotNet.Loggers;
using System;
using BenchmarkDotNet.Filters;

namespace Nethermind.Benchmark.Runner
{
    public class Options
    {
        [Option('m', "mode", Default = "full", Required = false, HelpText = "Available modes: full, bytecode")]
        public string Mode { get; set; }

        [Option('b', "bytecode", Required = false, HelpText = "Hex encoded bytecode")]
        public string ByteCode { get; set; }

    }

    public static class Program
    {

        public static void Main(string[] args)
        {
            ParserResult<Options> options = Parser.Default.ParseArguments<Options>(args);
            switch (options.Value.Mode)
            {
                case "full":
                    RunFullBenchmark(args);
                    break;
                case "bytecode":
                    RunBytecodeBenchmark(options.Value);
                    break;
                default:
                    throw new Exception("Invalid mode");
            }
        }

        public static void RunBytecodeBenchmark(Options options)
        {
            var config = new NoOutputConfig(
                Array.Empty<string>(),
                Job.LongRun.WithToolchain(InProcessNoEmitToolchain.DontLogOutput)
            );

            Environment.SetEnvironmentVariable("NETH.BENCHMARK.BYTECODE", options.ByteCode);
            var summary = BenchmarkRunner.Run<BytecodeBenchmark>(config);

            if (summary.HasCriticalValidationErrors)
            {
                var a = string.Join('|', summary.ValidationErrors.Select(ve => ve.Message));
                throw new Exception(a);
            }

            var report = summary.Reports[0];
            var executionTime = report.ResultStatistics?.Mean;
            var memAllocPerOp = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);

            Console.WriteLine($"{executionTime},{report.ResultStatistics.StandardDeviation},{report.GcStats.TotalOperations},{memAllocPerOp}");
        }

        static void RunFullBenchmark(string[] args)
        {
            List<Assembly> additionalJobAssemblies = new()
            {
                typeof(JsonRpc.Benchmark.EthModuleBenchmarks).Assembly,
                typeof(Benchmarks.Core.Keccak256Benchmarks).Assembly,
                typeof(Evm.Benchmark.EvmStackBenchmarks).Assembly,
                typeof(Network.Benchmarks.DiscoveryBenchmarks).Assembly,
                typeof(Precompiles.Benchmark.KeccakBenchmark).Assembly
            };

            List<Assembly> simpleJobAssemblies = new()
            {
                typeof(EthereumTests.Benchmark.EthereumTests).Assembly,
            };

            if (Debugger.IsAttached)
            {
                BenchmarkSwitcher.FromAssemblies(additionalJobAssemblies.Union(simpleJobAssemblies).ToArray()).RunAll(new DebugInProcessConfig());
            }
            else
            {
                foreach (Assembly assembly in additionalJobAssemblies)
                {
                    BenchmarkRunner.Run(assembly, new DashboardConfig(Job.MediumRun.WithRuntime(CoreRuntime.Core80)), args);
                }

                foreach (Assembly assembly in simpleJobAssemblies)
                {
                    BenchmarkRunner.Run(assembly, new DashboardConfig(), args);
                }
            }
        }
        public class DashboardConfig : ManualConfig
        {
            public DashboardConfig(params Job[] jobs)
            {
                foreach (Job job in jobs)
                {
                    AddJob(job.WithToolchain(InProcessNoEmitToolchain.Instance));
                }

                AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Descriptor);
                AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Statistics);
                AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Params);
                AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default);
                AddExporter(BenchmarkDotNet.Exporters.Json.JsonExporter.FullCompressed);
                AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
                WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(100));
            }
        }

        public class NoOutputConfig : ManualConfig
        {
            public NoOutputConfig(IEnumerable<string> filters, params Job[] jobs)
            {
                foreach (Job job in jobs)
                {
                    AddJob(job);
                }

                AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Statistics);
                AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Params);
                AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Metrics);
                AddLogger(new BenchmarkNullLogger());
                AddExporter(new BenchmarkNullExporter());
                AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
                WithOptions(ConfigOptions.DisableLogFile);

                if (filters.Any())
                {
                    IFilter[] nameFilters = filters.Select(a => new SimpleFilter(c => c.Parameters.Items.Any(p => p.Value.ToString().Contains(a)))).OfType<IFilter>().ToArray();
                    AddFilter(new DisjunctionFilter(nameFilters));
                }
            }
        }

        public class BenchmarkNullLogger : ILogger
        {
            public string Id => "ImappNullLogger";

            public int Priority => 1;

            public void Flush()
            {
            }

            public void Write(LogKind logKind, string text)
            {
            }

            public void WriteLine()
            {
            }

            public void WriteLine(LogKind logKind, string text)
            {
            }
        }

        public class BenchmarkNullExporter : BenchmarkDotNet.Exporters.IExporter
        {
            public string Name => "ImappNullExporter";

            public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
            {
                return Enumerable.Empty<string>();
            }

            public void ExportToLog(Summary summary, ILogger logger)
            {
            }
        }

    }
}
