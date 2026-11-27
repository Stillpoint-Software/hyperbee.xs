using System.Reflection;
using System.Runtime.InteropServices;
#if !NETSTANDARD
using System.Runtime.Loader;
using System.Text.RegularExpressions;
#endif
using Hyperbee.Collections;
using Hyperbee.XS.Internal;
#if !NETSTANDARD
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
#endif

namespace Hyperbee.XS.Core;

public partial class ReferenceManager
{
    public const string DefaultNuGetSource = "https://api.nuget.org/v3/index.json";

#if !NETSTANDARD
    private readonly string _globalPackagesFolder;
#endif
    private readonly XsAssemblyLoadContext _assemblyLoadContext = new();
    private readonly List<string> _nugetSources = [DefaultNuGetSource];

    private readonly ConcurrentSet<Assembly> _assemblyReferences =
    [
        typeof(string).Assembly,
        typeof(Enumerable).Assembly
    ];

    public static ReferenceManager Create( params Assembly[] references )
    {
        var referenceManager = new ReferenceManager();
        referenceManager.AddReference( references );
        return referenceManager;
    }

    public ReferenceManager()
    {
#if !NETSTANDARD
        var settings = Settings.LoadDefaultSettings( null );
        _globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder( settings );
#endif
    }

    public IEnumerable<Assembly> PackageAssemblies => _assemblyLoadContext.Assemblies;
    public IEnumerable<Assembly> ReferenceAssemblies => _assemblyReferences;
    public IEnumerable<Assembly> Assemblies => _assemblyReferences.Concat( _assemblyLoadContext.Assemblies );

    public void AddSource( string source )
    {
        // Assume newly added sources should be used first
        _nugetSources.Insert( 0, source );
    }

    public void AddSources( IEnumerable<string> sources )
    {
        // Assume newly added sources should be used first
        _nugetSources.InsertRange( 0, sources );
    }

    public void AddSources( params string[] sources )
    {
        // Assume newly added sources should be used first
        _nugetSources.InsertRange( 0, sources );
    }

    public ReferenceManager AddReference( Assembly assembly )
    {
        _assemblyReferences.Add( assembly );
        return this;
    }

    public ReferenceManager AddReference( IEnumerable<Assembly> assemblies )
    {
        _assemblyReferences.AddRange( assemblies );
        return this;
    }

    public ReferenceManager AddReference( params Assembly[] assemblies )
    {
        if ( assemblies == null || assemblies.Length == 0 )
            return this;

        _assemblyReferences.AddRange( assemblies );

        return this;
    }

    public Assembly LoadFromAssemblyPath( string assemblyPath )
    {
        // Get the assembly name from the path
        var assemblyName = AssemblyName.GetAssemblyName( assemblyPath );

        // Check if the assembly with the same name and version is already loaded
        var referenceAssembly = ReferenceAssemblies //_assemblyLoadContext.
            .FirstOrDefault( a => a.GetName().Name == assemblyName.Name && a.GetName().Version == assemblyName.Version );

        if ( referenceAssembly != null )
        {
            _assemblyLoadContext.LoadFromAssemblyPath( referenceAssembly.Location );
            return referenceAssembly;
        }

        return _assemblyLoadContext.LoadFromAssemblyPath( assemblyPath );
    }

    // Everything from here to the end of the NuGet region is compiled out of the
    // netstandard2.1 asset. NuGet.Configuration, NuGet.Protocol and NuGet.Packaging
    // ship lib/net472 and lib/net8.0 only, with no netstandard asset at any version,
    // so a netstandard2.1 build cannot reference them at all. The netstandard2.1
    // ReferenceManager therefore keeps assembly registration and path loading, which
    // is what Hyperbee.Templating's XS provider uses, and drops NuGet package
    // acquisition. Downlevel consumers that need `package` directives can reference
    // the net10.0 or net11.0 asset instead.
#if !NETSTANDARD

    public async Task<IEnumerable<Assembly>> LoadPackageAsync( string packageId, string version = default, string source = null, ILogger logger = default, CancellationToken cancellation = default )
    {
        version ??= "latest";

        if ( source == null )
        {
            var assemblies = new List<Assembly>();
            foreach ( var nugetSource in _nugetSources )
            {
                var packagePath = await GetPackageAsync(
                    packageId,
                    version,
                    nugetSource,
                    logger ?? NullLogger.Instance,
                    cancellation
                ).ConfigureAwait( false );

                if ( packagePath == null )
                    continue;

                assemblies.AddRange( LoadAssembliesFromPackage( packagePath ) );
            }

            if ( assemblies.Count == 0 )
                throw new InvalidOperationException( $"Failed to fetch package: {packageId}" );

            return assemblies;
        }
        else
        {
            var packagePath = await GetPackageAsync(
                packageId,
                version,
                source,
                logger ?? NullLogger.Instance,
                cancellation
            ).ConfigureAwait( false );

            if ( packagePath == null )
                throw new InvalidOperationException( $"Failed to fetch package: {packageId}" );

            return LoadAssembliesFromPackage( packagePath );
        }
    }

    private async Task<string> GetPackageAsync( string packageId, string version, string source, ILogger logger, CancellationToken cancellation )
    {
        var packageRepository = Repository.Factory.GetCoreV3( source );

        var packageResource = await GetPackageResourceAsync( packageRepository, cancellation )
            .ConfigureAwait( false );

        var resolvedPackages = await ResolvePackageDependenciesAsync(
            packageRepository,
            packageId,
            version,
            logger,
            cancellation
        ).ConfigureAwait( false );

        string packageIdFolder = null;

        foreach ( var package in resolvedPackages )
        {
            var packageFolder = Path.Combine( _globalPackagesFolder, package.Id.ToLower(), package.Version.ToString() );

            if ( package.Id.Equals( packageId, StringComparison.OrdinalIgnoreCase ) )
            {
                packageIdFolder = packageFolder;
            }

            if ( Directory.Exists( packageFolder ) )
            {
                break;
            }

            var packagePath = await DownloadPackageAsync(
                packageResource,
                packageFolder,
                package,
                cancellation
            ).ConfigureAwait( false );

            await ExtractPackageAsync(
                packagePath,
                cancellation
            ).ConfigureAwait( false );
        }

        return packageIdFolder;
    }

    private static async Task<FindPackageByIdResource> GetPackageResourceAsync( SourceRepository repository, CancellationToken cancellation )
    {
        return await repository.GetResourceAsync<FindPackageByIdResource>( cancellation )
            .ConfigureAwait( false );
    }

    private static async Task<List<PackageIdentity>> ResolvePackageDependenciesAsync( SourceRepository repository, string packageId, string version, ILogger logger, CancellationToken cancellation )
    {
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>( cancellation )
            .ConfigureAwait( false );

        var versions = await metadataResource.GetMetadataAsync(
            packageId,
            true,
            false,
            NullSourceCacheContext.Instance,
            logger,
            cancellation
        ).ConfigureAwait( false );

        var packageMetadata = (version == "latest")
            ? versions
                .Where( m => !m.Identity.Version.IsPrerelease )
                .MaxBy( m => m.Identity.Version )
            : versions.FirstOrDefault( m => m.Identity.Version == NuGetVersion.Parse( version ) );

        if ( packageMetadata == null )
            return [];

        var identities = new List<PackageIdentity> { packageMetadata.Identity };

        foreach ( var dependency in packageMetadata.DependencySets.SelectMany( ds => ds.Packages ) )
        {
            if ( dependency.VersionRange.MinVersion != null )
            {
                identities.Add( new PackageIdentity( dependency.Id, dependency.VersionRange.MinVersion ) );
            }
        }

        return identities;
    }

    private static async Task<string> DownloadPackageAsync( FindPackageByIdResource packageResource, string packageFolder, PackageIdentity package, CancellationToken cancellation )
    {
        Directory.CreateDirectory( packageFolder );

        var packagePath = Path.Combine( packageFolder, $"{package.Id}.{package.Version}.nupkg" );

        if ( !File.Exists( packagePath ) )
        {
            await using var packageStream = File.Create( packagePath );

            await packageResource.CopyNupkgToStreamAsync(
                package.Id,
                package.Version,
                packageStream,
                NullSourceCacheContext.Instance,
                NullLogger.Instance,
                cancellation
            ).ConfigureAwait( false );
        }

        return packagePath;
    }

    private static async Task ExtractPackageAsync( string packagePath, CancellationToken cancellation )
    {
        using var packageReader = new PackageArchiveReader( packagePath );

        var packageFolder = Path.GetDirectoryName( packagePath )!;
        var packageFiles = await packageReader.GetFilesAsync( cancellation ).ConfigureAwait( false );

        foreach ( var file in packageFiles )
        {
            var targetPath = Path.Combine( packageFolder, file );
            var directory = Path.GetDirectoryName( targetPath );

            if ( !string.IsNullOrEmpty( directory ) && !Directory.Exists( directory ) )
            {
                Directory.CreateDirectory( directory );
            }

            await using var fileStream = File.Create( targetPath );
            await using var packageStream = packageReader.GetStream( file );
            await packageStream.CopyToAsync( fileStream, cancellation ).ConfigureAwait( false );
        }
    }

    private List<Assembly> LoadAssembliesFromPackage( string packageFolder )
    {
        var libPath = Path.Combine( packageFolder, "lib" );
        if ( !Directory.Exists( libPath ) )
            return [];

        var availableFrameworks = Directory.GetDirectories( libPath )
            .Select( Path.GetFileName )
            .Where( name => name.StartsWith( "net", StringComparison.OrdinalIgnoreCase ) )
            .ToList();

        if ( availableFrameworks.Count == 0 )
            return [];

        var selectedFramework = SelectBestMatchingFramework( availableFrameworks );
        var selectedLibPath = Path.Combine( libPath, selectedFramework );

        if ( !Directory.Exists( selectedLibPath ) )
            return [];

        var assemblies = new List<Assembly>();
        foreach ( var assemblyPath in Directory.GetFiles( selectedLibPath, "*.dll", SearchOption.AllDirectories ) )
        {
            var assembly = _assemblyLoadContext.LoadFromAssemblyPath( assemblyPath );
            assemblies.Add( assembly );
        }

        return assemblies;
    }

    private static string SelectBestMatchingFramework( List<string> availableFrameworks )
    {
        var currentRuntime = GetCurrentRuntimeVersion();
        return availableFrameworks
            .Where( x => x.StartsWith( currentRuntime ) )
            .OrderByDescending( x => x )
            .FirstOrDefault() ?? availableFrameworks.Last();
    }

    private static string GetCurrentRuntimeVersion()
    {
        var framework = RuntimeInformation.FrameworkDescription;
        var match = NetVersionRegex().Match( framework );
        return match.Success && int.TryParse( match.Groups[1].Value, out int version ) && version >= 8
            ? $"net{version}.0"
            : "net8.0";
    }

    [GeneratedRegex( @"\.NET (\d+)" )]
    private static partial Regex NetVersionRegex();

#endif // !NETSTANDARD

#if !NETSTANDARD

    private class XsAssemblyLoadContext : AssemblyLoadContext
    {
        public XsAssemblyLoadContext() : base( "XS-Context", isCollectible: true ) { }

        protected override Assembly Load( AssemblyName assemblyName )
        {
            return Default.Assemblies.FirstOrDefault( x => x.GetName().FullName == assemblyName.FullName );
        }
    }

#else

    /// <summary>
    /// Stands in for <c>AssemblyLoadContext</c>, which netstandard2.1 does not carry.
    /// </summary>
    /// <remarks>
    /// The netstandard2.1 contract exposes no usable <c>System.Runtime.Loader</c>: the
    /// named, collectible constructor, the <c>Assemblies</c> property and the
    /// <c>Load</c> override all arrived with .NET Core 3.0 and are not part of the
    /// standard. This shim keeps the two members ReferenceManager actually calls,
    /// backed by <see cref="Assembly.LoadFrom(string)"/> into the default context.
    /// Scripts loaded through the netstandard2.1 asset are therefore not isolated and
    /// not collectible. That is acceptable here because the netstandard2.1 asset also
    /// has no NuGet package acquisition, so the only assemblies it loads are ones the
    /// host handed it by path.
    /// </remarks>
    private class XsAssemblyLoadContext
    {
        private readonly List<Assembly> _assemblies = [];

        public IEnumerable<Assembly> Assemblies => _assemblies;

        public Assembly LoadFromAssemblyPath( string assemblyPath )
        {
            var assembly = Assembly.LoadFrom( assemblyPath );

            if ( !_assemblies.Contains( assembly ) )
                _assemblies.Add( assembly );

            return assembly;
        }
    }

#endif
}
