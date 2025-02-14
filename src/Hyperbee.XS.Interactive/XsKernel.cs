using Hyperbee.Collections;
using Hyperbee.Collections.Extensions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;

using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Interactive;

public class XsKernel() : 
    XsBaseKernel( "xs" ),
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>
{
    Task IKernelCommandHandler<SubmitCode>.HandleAsync( SubmitCode command, KernelInvocationContext context )
    {
        try
        { 
            var parser = new XsParser( Config );

            Scope.Variables.Push();

            var expression = parser.Parse( command.Code, scope: Scope );

            foreach( var variable in Scope.Variables )
            {
                if(Values.TryGetValue( variable.Key, out var value ) )
                {
                    if( value.GetType() != variable.Value.Type ) { 
                        Values.Remove(variable.Key);
                    }
                }
            }

            var wrapExpression = WrapWithPersistentState( expression, Scope.Variables, Values );

            var delegateType = expression.Type == typeof( void )
                ? typeof( Action )
                : typeof( Func<> ).MakeGenericType( expression.Type );

            var lambda = Lambda( delegateType, wrapExpression );
            var compiled = lambda.Compile();
            var result = compiled.DynamicInvoke()?.ToString() ?? "null";

            // Update the variables in the current scope with the new values
            var newVariables = Scope.Variables.Pop().Dictionary;
            foreach ( var variable in newVariables )
            {
                Scope.Variables[variable.Key] = variable.Value;
            }

            result.Display( PlainTextFormatter.MimeType );
        }
        catch ( Exception ex )
        {
            context.Fail( command, message: ex.Message );
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync( RequestValue command, KernelInvocationContext context )
    {
        if ( Values.TryGetValue( command.Name, out var value ) )
        {
            context.PublishValueProduced( command, value );
        }
        else
        {
            context.Fail( command, message: $"Value '{command.Name}' not found in kernel {Name}" );
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync( RequestValueInfos command, KernelInvocationContext context )
    {
        try
        {
            var valueInfos = Values
            .Select( kvp =>
            {
                var formattedValues = FormattedValue.CreateSingleFromObject(
                    kvp.Value,
                    command.MimeType );

                return new KernelValueInfo(
                    kvp.Key,
                    formattedValues,
                    kvp.Value.GetType() );
            } )
            .ToArray() ?? [];

            context.Publish( new ValueInfosProduced( valueInfos, command ) );
        }
        catch(Exception ex )
        {
            context.Fail(command, ex);
        }

        return Task.CompletedTask;
    }

    async Task IKernelCommandHandler<SendValue>.HandleAsync( SendValue command, KernelInvocationContext context )
    {
        try
        { 
            await SetValueAsync( command, context, SetValueAsync );
        }
        catch( Exception ex )
        {
            context.Fail( command, ex );
        }
    }

    public Task SetValueAsync( string name, object value, Type declaredType )
    {
        var type = declaredType ?? value.GetType();

        Scope.Variables[LinkedNode.Current,name] = Parameter( type, name );

        Values[name] = value;

        return Task.CompletedTask;
    }
}
