using Spectre.Console.Cli;

namespace SharkyParser.Cli.Infrastructure;

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
        => type == null ? null : _provider.GetService(type);
}
