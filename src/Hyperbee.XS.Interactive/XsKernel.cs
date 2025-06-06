﻿using System.Globalization;
using Hyperbee.Collections;
using Hyperbee.XS.Core;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;

using static System.Linq.Expressions.Expression;

namespace Hyperbee.XS.Interactive;

public class XsKernel : XsBaseKernel,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>
{
    public XsKernel() : base( "xs" )
    {
        KernelInfo.LanguageName = "XS";
        KernelInfo.Description = "Compile and run Expression Script";
    }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync( SubmitCode command, KernelInvocationContext context )
    {
        try
        {
            var result = Parser.Value
                .ParseWithState( command.Code, Scope, State )
                .InvokeWithState( Scope );

            (result?.ToString() ?? "null").Display( PlainTextFormatter.MimeType );
        }
        catch ( Exception ex )
        {
            context.Fail( command, message: ex.Message );
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync( RequestValue command, KernelInvocationContext context )
    {
        if ( State.TryGetValue( command.Name, out var value ) )
        {
            PublishValueProduced( context, command, value );
        }
        else
        {
            context.Fail( command, message: $"Value '{command.Name}' not found in kernel {Name}" );
        }

        return Task.CompletedTask;

        static void PublishValueProduced(
            KernelInvocationContext context,
            RequestValue requestValue,
            object value )
        {
            var valueType = value?.GetType();

            var requestedMimeType = requestValue.MimeType;

            var formatter = Formatter.GetPreferredFormatterFor( valueType, requestedMimeType );

            using var writer = new StringWriter( CultureInfo.InvariantCulture );
            formatter.Format( value, writer );

            var formatted = new FormattedValue(
                requestedMimeType,
                writer.ToString() );

            context.Publish( new ValueProduced(
                value,
                requestValue.Name,
                formatted,
                requestValue ) );
        }
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync( RequestValueInfos command, KernelInvocationContext context )
    {
        try
        {
            var valueInfos = State
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
                .ToArray();

            context.Publish( new ValueInfosProduced( valueInfos, command ) );
        }
        catch ( Exception ex )
        {
            context.Fail( command, ex );
        }

        return Task.CompletedTask;
    }

    async Task IKernelCommandHandler<SendValue>.HandleAsync( SendValue command, KernelInvocationContext context )
    {
        try
        {
            await SetValueAsync( command, context, SetValueAsync );
        }
        catch ( Exception ex )
        {
            context.Fail( command, ex );
        }
    }

    public Task SetValueAsync( string name, object value, Type declaredType )
    {
        var type = declaredType ?? value.GetType();

        Scope.Variables[LinkedNode.Current, name] = Parameter( type, name );

        State[name] = value;

        return Task.CompletedTask;
    }
}
