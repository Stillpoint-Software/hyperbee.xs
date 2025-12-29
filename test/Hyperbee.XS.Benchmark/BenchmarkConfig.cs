using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Validators;

namespace Hyperbee.XS.Benchmark;

public class BenchmarkConfig
{
    public class Config : ManualConfig
    {
        public Config()
        {
            AddJob( Job.ShortRun
           .WithRuntime( CoreRuntime.Core80 )
           .WithId( ".NET 8" ) );

            AddJob( Job.ShortRun
                .WithRuntime( CoreRuntime.Core90 )
                .WithId( ".NET 9" ) );

            AddExporter( MarkdownExporter.GitHub );
            AddValidator( JitOptimizationsValidator.DontFailOnError );
            AddLogger( ConsoleLogger.Default );
            AddColumnProvider(
                DefaultColumnProviders.Job,
                DefaultColumnProviders.Params,
                DefaultColumnProviders.Descriptor,
                DefaultColumnProviders.Metrics,
                DefaultColumnProviders.Statistics
            );

            AddDiagnoser( MemoryDiagnoser.Default );

            AddLogicalGroupRules( BenchmarkLogicalGroupRule.ByCategory );

            Orderer = new DefaultOrderer( SummaryOrderPolicy.FastestToSlowest );
            ArtifactsPath = "benchmark";
        }
    }
}
