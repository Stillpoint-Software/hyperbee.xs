using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.Collections;

namespace Hyperbee.XS.Core;

public sealed class TypeResolver
{
    public ReferenceManager ReferenceManager { get; }

    private readonly ConcurrentDictionary<string, Type> _typeCache = [];
    private readonly ConcurrentDictionary<(MethodInfo, IReadOnlyList<Type>), MethodInfo> _genericResolutionCache = [];

    private readonly ConcurrentDictionary<string, ConcurrentSet<MethodInfo>> _extensionMethodCache = [];
    private readonly ConcurrentSet<Assembly> _extensionAssemblyCache = [];

    private static readonly ConcurrentDictionary<Type, bool> __nullableTypeCache = [];

    private static readonly Dictionary<Type, HashSet<Type>> WideningConversions = new()
    {
        { typeof(byte), [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(sbyte), [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(short), [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(ushort), [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(int), [typeof(long), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(uint), [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(long), [typeof(float), typeof(double), typeof(decimal)] },
        { typeof(ulong), [typeof(float), typeof(double), typeof(decimal)] },
        { typeof(char), [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)] },
        { typeof(float), [typeof(double)] }
    };

    public static TypeResolver Create( ReferenceManager referenceManager )
    {
        var typeResolver = new TypeResolver( referenceManager );

        typeResolver.RegisterExtensionMethods( referenceManager.Assemblies );

        return typeResolver;
    }

    public static TypeResolver Create( params Assembly[] references )
    {
        return Create( ReferenceManager.Create( references ) );
    }

    public TypeResolver( ReferenceManager referenceManager )
    {
        ArgumentNullException.ThrowIfNull( referenceManager, nameof( referenceManager ) );

        ReferenceManager = referenceManager;
    }

    public Type ResolveType( string typeName )
    {
        return _typeCache.GetOrAdd( typeName, _ =>
        {
            var type = GetTypeFromKeyword( typeName );

            if ( type != null )
                return type;

            return ReferenceManager.Assemblies
                .SelectMany( assembly => assembly.GetTypes() )
                .FirstOrDefault( compare => compare.Name == typeName || compare.FullName == typeName );
        } );

        static Type GetTypeFromKeyword( string typeName )
        {
            // Mapping of C# keywords to their corresponding types
            return typeName switch
            {
                "int" => typeof( int ),
                "double" => typeof( double ),
                "string" => typeof( string ),
                "bool" => typeof( bool ),
                "float" => typeof( float ),
                "decimal" => typeof( decimal ),
                "object" => typeof( object ),
                "byte" => typeof( byte ),
                "char" => typeof( char ),
                "short" => typeof( short ),
                "long" => typeof( long ),
                "uint" => typeof( uint ),
                "ushort" => typeof( ushort ),
                "ulong" => typeof( ulong ),
                "sbyte" => typeof( sbyte ),
                "void" => typeof( void ),
                _ => null, // Return null for unknown types
            };
        }
    }

    public MethodInfo ResolveMethod( Type type, string methodName, IReadOnlyList<Type> typeArgs, IReadOnlyList<Expression> args )
    {
        var candidateMethods = GetCandidateMethods( methodName, type );
        var callerTypes = GetCallerTypes( type, args );

        typeArgs ??= [];

        MethodInfo bestMatch = null;
        var ambiguousMatch = false;

        foreach ( var candidate in candidateMethods )
        {
            var isExtension = candidate.IsDefined( typeof( ExtensionAttribute ), false );

            var argumentTypes = isExtension ? callerTypes : callerTypes[1..]; // extension methods have the first argument as the caller
            MethodInfo method = candidate;

            if ( candidate.IsGenericMethodDefinition )
            {
                if ( !_genericResolutionCache.TryGetValue( (candidate, typeArgs), out method ) )
                {
                    if ( !TryResolveGenericDefinition( candidate, typeArgs, argumentTypes, out method ) )
                        continue;

                    _genericResolutionCache[(candidate, typeArgs)] = method;
                }
            }

            var parameters = method.GetParameters();

            if ( !IsApplicable( parameters, argumentTypes ) )
                continue;

            if ( bestMatch != null )
            {
                var compare = CompareMethods( bestMatch, method );

                switch ( compare )
                {
                    case > 0:
                        bestMatch = method;
                        ambiguousMatch = false;
                        break;
                    case 0:
                        ambiguousMatch = true;
                        break;
                }
            }
            else
            {
                bestMatch = method;
            }
        }

        if ( ambiguousMatch )
            throw new AmbiguousMatchException( $"Ambiguous match for method '{methodName}'." );

        return bestMatch;
    }

    public void RegisterExtensionMethods( IEnumerable<Assembly> assemblies )
    {
        Parallel.ForEach( assemblies, assembly =>
        {
            if ( !_extensionAssemblyCache.TryAdd( assembly ) )
            {
                return; // Already processed
            }

            foreach ( var type in assembly.GetTypes() )
            {
                if ( !type.IsPublic || !type.IsSealed || !type.IsAbstract ) // Only static classes
                    continue;

                foreach ( var method in type.GetMethods( BindingFlags.Static | BindingFlags.Public ) )
                {
                    if ( !method.IsDefined( typeof( ExtensionAttribute ), false ) )
                        continue;

                    var methods = _extensionMethodCache.GetOrAdd( method.Name, _ => [] );
                    methods.TryAdd( method );
                }
            }
        } );
    }

    private static Span<Type> GetCallerTypes( Type type, IReadOnlyList<Expression> args )
    {
        var types = new Type[args.Count + 1].AsSpan();
        types[0] = type;

        for ( var i = 0; i < args.Count; i++ )
        {
            types[i + 1] = args[i] is ConstantExpression constant ? constant.Value?.GetType() : args[i].Type;
        }

        return types;
    }

    private IEnumerable<MethodInfo> GetCandidateMethods( string methodName, Type type )
    {
        var extensionMethods = _extensionMethodCache.TryGetValue( methodName, out var extensions )
            ? extensions
            : Enumerable.Empty<MethodInfo>();

        return type.GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static )
            .Where( method => method.Name == methodName )
            .Concat( extensionMethods );
    }

    private static bool IsApplicable( ParameterInfo[] parameters, ReadOnlySpan<Type> argumentTypes )
    {
        for ( var i = 0; i < argumentTypes.Length; i++ )
        {
            if ( i >= parameters.Length )
                return parameters[^1].IsDefined( typeof( ParamArrayAttribute ), false );

            var paramType = parameters[i].ParameterType;
            var argType = argumentTypes[i];

            if ( argType == null )
            {
                if ( !paramType.IsClass && !__nullableTypeCache.GetOrAdd( paramType, x => Nullable.GetUnderlyingType( x ) != null ) )
                    return false;
            }
            else if ( !paramType.IsAssignableFrom( argType ) && !IsWideningConversion( argType, paramType ) )
            {
                return false;
            }
        }

        for ( var i = argumentTypes.Length; i < parameters.Length; i++ )
        {
            if ( !parameters[i].IsOptional )
                return false;
        }

        return true;
    }

    private static bool IsWideningConversion( Type from, Type to )
    {
        return WideningConversions.TryGetValue( from, out var targets ) && targets.Contains( to );
    }

    private bool TryResolveGenericDefinition( MethodInfo method, IReadOnlyList<Type> typeArgs, ReadOnlySpan<Type> argumentTypes, out MethodInfo resolvedMethod )
    {
        resolvedMethod = method;

        if ( _genericResolutionCache.TryGetValue( (method, typeArgs), out var cachedResult ) )
        {
            resolvedMethod = cachedResult;
            return true;
        }

        if ( typeArgs.Count == 0 )
        {
            typeArgs = InferGenericArguments( method, argumentTypes );

            if ( typeArgs == null )
                return false;
        }

        if ( method.GetGenericArguments().Length != typeArgs.Count )
        {
            return false;
        }

        try
        {
            resolvedMethod = method.MakeGenericMethod( [.. typeArgs] );
        }
        catch
        {
            return false;
        }

        _genericResolutionCache[(method, typeArgs)] = resolvedMethod;
        return true;
    }

    private static Type[] InferGenericArguments( MethodInfo method, ReadOnlySpan<Type> argumentTypes )
    {
        var genericParameters = method.GetGenericArguments();
        var inferredTypes = new Type[genericParameters.Length];
        var parameters = method.GetParameters();

        for ( var i = 0; i < parameters.Length; i++ )
        {
            if ( i >= argumentTypes.Length )
            {
                if ( !parameters[i].HasDefaultValue )
                    return null; // Missing argument for non-default parameter

                continue; // Skip inference for default parameters
            }

            if ( !TryInferTypes( parameters[i].ParameterType, argumentTypes[i], genericParameters, inferredTypes ) )
                return null;
        }

        return inferredTypes;
    }

    private static bool TryInferTypes( Type parameterType, Type argumentType, Type[] genericParameters, Type[] inferredTypes )
    {
        while ( true )
        {
            // Handle direct generic parameters

            if ( parameterType.IsGenericParameter )
            {
                var index = Array.IndexOf( genericParameters, parameterType );

                if ( index < 0 )
                    return true;

                switch ( inferredTypes[index] )
                {
                    case null:
                        inferredTypes[index] = argumentType;
                        break;
                    default:
                        if ( inferredTypes[index] != argumentType )
                            return false;
                        break;
                }

                return true;
            }

            // Handle array types explicitly for IEnumerable<T>

            if ( parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof( IEnumerable<> ) && argumentType!.IsArray )
            {
                var elementType = argumentType.GetElementType();
                var genericArg = parameterType.GetGenericArguments()[0];

                parameterType = genericArg;
                argumentType = elementType;
                continue;
            }

            // Handle nested generic types

            if ( !parameterType.ContainsGenericParameters )
                return true;

            if ( !parameterType.IsGenericType || !argumentType!.IsGenericType ||
                 parameterType.GetGenericTypeDefinition() != argumentType.GetGenericTypeDefinition() )
                return false;

            var paramArgs = parameterType.GetGenericArguments();
            var argArgs = argumentType.GetGenericArguments();

            for ( var i = 0; i < paramArgs.Length; i++ )
            {
                if ( !TryInferTypes( paramArgs[i], argArgs[i], genericParameters, inferredTypes ) )
                    return false;
            }

            return true;
        }
    }

    private static int CompareMethods( MethodInfo m1, MethodInfo m2 )
    {
        switch ( m1.IsGenericMethod )
        {
            case false when m2.IsGenericMethod:
                return -1;

            case true when !m2.IsGenericMethod:
                return 1;
        }

        var p1 = m1.GetParameters();
        var p2 = m2.GetParameters();

        for ( var i = 0; i < Math.Min( p1.Length, p2.Length ); i++ )
        {
            if ( p1[i].ParameterType == p2[i].ParameterType )
                continue;

            if ( p1[i].ParameterType.IsAssignableFrom( p2[i].ParameterType ) )
                return 1;

            if ( p2[i].ParameterType.IsAssignableFrom( p1[i].ParameterType ) )
                return -1;
        }

        return p1.Length.CompareTo( p2.Length );
    }
}

