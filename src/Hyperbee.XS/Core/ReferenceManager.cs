using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Hyperbee.Collections;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Hyperbee.XS.Core;

public partial class ReferenceManager
{
    private const string NuGetSource = "https://api.nuget.org/v3/index.json";

    private readonly string _globalPackagesFolder;
    private readonly XsAssemblyLoadContext _assemblyLoadContext = new();

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
        var settings = Settings.LoadDefaultSettings( null );
        _globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder( settings );
    }

    public IEnumerable<Assembly> PackageAssemblies => _assemblyLoadContext.Assemblies;
    public IEnumerable<Assembly> ReferenceAssemblies => _assemblyReferences;
    public IEnumerable<Assembly> Assemblies => _assemblyReferences.Concat( _assemblyLoadContext.Assemblies );

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
        return _assemblyLoadContext.LoadFromAssemblyPath( assemblyPath );
    }

    public async Task<IEnumerable<Assembly>> LoadPackageAsync( string packageId, string version = default, ILogger logger = default, CancellationToken cancellation = default )
    {
        version ??= "latest";

        var packagePath = await GetPackageAsync(
            packageId,
            version,
            logger ?? NullLogger.Instance,
            cancellation
        ).ConfigureAwait( false );

        if ( packagePath == null )
            throw new InvalidOperationException( $"Failed to fetch package: {packageId}" );

        return LoadAssembliesFromPackage( packagePath );
    }

    private async Task<string> GetPackageAsync( string packageId, string version, ILogger logger, CancellationToken cancellation )
    {
        var packageResource = await GetPackageResourceAsync( cancellation )
            .ConfigureAwait( false );

        var resolvedPackages = await ResolvePackageDependenciesAsync(
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
                continue;
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

    private static async Task<FindPackageByIdResource> GetPackageResourceAsync( CancellationToken cancellation )
    {
        var repository = Repository.Factory.GetCoreV3( NuGetSource );

        return await repository.GetResourceAsync<FindPackageByIdResource>( cancellation )
            .ConfigureAwait( false );
    }

    private static async Task<List<PackageIdentity>> ResolvePackageDependenciesAsync( string packageId, string version, ILogger logger, CancellationToken cancellation )
    {
        var repository = Repository.Factory.GetCoreV3( NuGetSource );

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
            throw new InvalidOperationException( $"Package metadata not found for {packageId} ({version})" );

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

    private class XsAssemblyLoadContext : AssemblyLoadContext
    {
        public XsAssemblyLoadContext() : base( "XS-Context", isCollectible: true ) { }

        protected override Assembly Load( AssemblyName assemblyName )
        {
            return Default.Assemblies.FirstOrDefault( x => x.GetName().FullName == assemblyName.FullName );
        }
    }
}
