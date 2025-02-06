using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Hyperbee.XS.Core;

public partial class ReferenceManager
{
    private const string NuGetSource = "https://api.nuget.org/v3/index.json";

    private readonly string _cachePath;
    private readonly string _globalPackagesFolder;

    private readonly XsAssemblyLoadContext _assemblyLoadContext = new();

    private readonly List<Assembly> _assemblyReferences =
    [
        typeof(string).Assembly,
        typeof(Enumerable).Assembly
    ];

    private readonly List<string> _loadedPackages = [];

    public static ReferenceManager Create( params Assembly[] references )
    {
        var referenceManager = new ReferenceManager();
        referenceManager.AddReference( references );

        return referenceManager;
    }

    public ReferenceManager( string cachePath = null )
    {
        _cachePath = cachePath ?? Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".nuget", "xs-packages" );

        try
        {
            if ( !Directory.Exists( _cachePath ) )
            {
                Directory.CreateDirectory( _cachePath );
            }
        }
        catch ( Exception ex )
        {
            throw new InvalidOperationException( $"Failed to create cache directory: {_cachePath}", ex );
        }

        var settings = Settings.LoadDefaultSettings( null );
        _globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder( settings );
    }

    public IEnumerable<string> Packages => _loadedPackages;

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

    public Assembly LoadFromAssemblyName( AssemblyName assemblyName )
    {
        return _assemblyLoadContext.LoadFromAssemblyName( assemblyName );
    }

    public Assembly LoadFromStream( Stream assembly )
    {
        return _assemblyLoadContext.LoadFromStream( assembly );
    }

    public async Task<IEnumerable<Assembly>> LoadPackage( string packageId, string version = "latest" )
    {
        var packagePath = await GetPackage( packageId, version ).ConfigureAwait( false );
        if ( packagePath == null )
            throw new InvalidOperationException( $"Failed to fetch package: {packageId}" );

        _loadedPackages.Add( $"{packageId} {version}" );
        return LoadAssembliesFromPackage( packagePath );
    }

    private async Task<string> GetPackage( string packageId, string version, HashSet<string> processedPackages = null )
    {
        processedPackages ??= [];

        if ( !processedPackages.Add( $"{packageId}@{version}" ) )
            return null;

        var cachedPackage = FindCachedPackage( packageId, version );
        if ( cachedPackage != null )
            return cachedPackage;

        var availableVersions = await FindAvailableVersions( packageId ).ConfigureAwait( false );
        var selectedVersion = version == "latest"
            ? availableVersions.Where( v => !v.IsPrerelease ).Max()
            : NuGetVersion.Parse( version );

        cachedPackage = FindCachedPackage( packageId, selectedVersion.ToString() );
        if ( cachedPackage != null )
            return cachedPackage;

        var packageFolder = await DownloadPackage( packageId, selectedVersion ).ConfigureAwait( false );

        var assemblies = LoadAssembliesFromPackage( packageFolder );
        foreach ( var assembly in assemblies )
        {
            _assemblyLoadContext.LoadFromAssemblyPath( assembly.Location );
        }

        var dependencies = await GetPackageDependencies( packageId, selectedVersion ).ConfigureAwait( false );
        foreach ( var dependency in dependencies )
        {
            await GetPackage( dependency.Id, dependency.Version.ToNormalizedString(), processedPackages ).ConfigureAwait( false );
        }

        return packageFolder;
    }

    private static async Task<List<PackageIdentity>> GetPackageDependencies( string packageId, NuGetVersion version )
    {
        var repository = Repository.Factory.GetCoreV3( NuGetSource );
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();

        var metadata = await metadataResource.GetMetadataAsync( packageId, true, false, NullSourceCacheContext.Instance, NullLogger.Instance, CancellationToken.None );
        var packageMetadata = metadata.FirstOrDefault( m => m.Identity.Version == version );

        if ( packageMetadata == null )
            return [];

        var dependencies = new List<PackageIdentity>();

        foreach ( var dependency in packageMetadata.DependencySets.SelectMany( ds => ds.Packages ) )
        {
            // Only add direct dependencies, do not resolve sub-dependencies
            if ( dependency.VersionRange.MinVersion != null )
            {
                dependencies.Add( new PackageIdentity( dependency.Id, dependency.VersionRange.MinVersion ) );
            }
        }

        return dependencies;
    }

    private string FindCachedPackage( string packageId, string version )
    {
        var globalPackageFolder = Path.Combine( _globalPackagesFolder, packageId.ToLower(), version );

        if ( Directory.Exists( globalPackageFolder ) )
            return globalPackageFolder;

        var localCacheFolder = Path.Combine( _cachePath, packageId.ToLower(), version );

        return Directory.Exists( localCacheFolder )
            ? localCacheFolder
            : null;
    }

    private static async Task<NuGetVersion[]> FindAvailableVersions( string packageId )
    {
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository( new PackageSource( NuGetSource ), providers );
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>().ConfigureAwait( false );

        var metadata = await metadataResource.GetMetadataAsync( packageId, true, false, NullSourceCacheContext.Instance, NullLogger.Instance, new CancellationToken() ).ConfigureAwait( false );
        return metadata.Select( m => m.Identity.Version ).ToArray();
    }

    private async Task<string> DownloadPackage( string packageId, NuGetVersion version )
    {
        var repository = Repository.Factory.GetCoreV3( NuGetSource );
        var packageResource = await repository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait( false );

        var packageFolder = Path.Combine( _cachePath, packageId.ToLower(), version.ToString() );
        Directory.CreateDirectory( packageFolder );

        var packagePath = Path.Combine( packageFolder, $"{packageId}.{version}.nupkg" );

        if ( File.Exists( packagePath ) )
        {
            return packageFolder;
        }

        var packageStream = File.Create( packagePath );
        await using ( packageStream.ConfigureAwait( false ) )
        {
            await packageResource.CopyNupkgToStreamAsync( packageId, version, packageStream, NullSourceCacheContext.Instance, NullLogger.Instance, new CancellationToken() ).ConfigureAwait( false );
        }

        ExtractPackage( packagePath, packageFolder );
        return packageFolder;
    }

    private static void ExtractPackage( string packagePath, string destinationFolder )
    {
        if ( !File.Exists( packagePath ) )
            throw new FileNotFoundException( $"Package file not found: {packagePath}" );

        ZipFile.ExtractToDirectory( packagePath, destinationFolder, overwriteFiles: true );
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

        foreach ( var dll in Directory.GetFiles( selectedLibPath, "*.dll", SearchOption.AllDirectories ) )
        {
            var assembly = _assemblyLoadContext.LoadFromAssemblyPath( dll );
            assemblies.Add( assembly );
        }

        return assemblies;
    }

    private static string SelectBestMatchingFramework( List<string> availableFrameworks )
    {
        var currentRuntime = GetCurrentRuntimeVersion();

        var bestMatch = availableFrameworks
            .Where( v => v.StartsWith( currentRuntime ) )
            .OrderByDescending( v => v )
            .FirstOrDefault();

        return bestMatch ?? availableFrameworks.Last();
    }

    private static string GetCurrentRuntimeVersion()
    {
        var framework = RuntimeInformation.FrameworkDescription;
        var match = NetVersionRegex().Match( framework );

        if ( match.Success && int.TryParse( match.Groups[1].Value, out int version ) && version >= 8 )
        {
            return $"net{version}.0";
        }

        return "net8.0";
    }

    [GeneratedRegex( @"\.NET (\d+)" )]
    private static partial Regex NetVersionRegex();

    private class XsAssemblyLoadContext : AssemblyLoadContext
    {
        public XsAssemblyLoadContext() : base( "XS-Context", isCollectible: true ) { }

        protected override Assembly Load( AssemblyName assemblyName )
        {
            var existingAssembly = Default.Assemblies
                .FirstOrDefault( a => a.GetName().FullName == assemblyName.FullName );

            return existingAssembly != null
                ? existingAssembly
                : null; // Delegate back to default Load behavior (fallback)
        }
    }
}
