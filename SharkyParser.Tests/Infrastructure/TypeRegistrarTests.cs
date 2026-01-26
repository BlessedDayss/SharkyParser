using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Infrastructure;

namespace SharkyParser.Tests.Infrastructure;

public class TypeRegistrarTests
{
    [Fact]
    public void Register_ResolvesImplementation()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        registrar.Register(typeof(IFoo), typeof(Foo));
        var resolver = registrar.Build();

        resolver.Resolve(typeof(IFoo)).Should().BeOfType<Foo>();
    }

    [Fact]
    public void RegisterInstance_ResolvesSameInstance()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var instance = new Foo();

        registrar.RegisterInstance(typeof(IFoo), instance);
        var resolver = registrar.Build();

        resolver.Resolve(typeof(IFoo)).Should().BeSameAs(instance);
    }

    [Fact]
    public void RegisterLazy_ResolvesSingletonFromFactory()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var callCount = 0;

        registrar.RegisterLazy(typeof(IFoo), () =>
        {
            callCount++;
            return new Foo();
        });

        var resolver = registrar.Build();
        var first = resolver.Resolve(typeof(IFoo));
        var second = resolver.Resolve(typeof(IFoo));

        callCount.Should().Be(1);
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Resolve_WithNullType_ReturnsNull()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var resolver = registrar.Build();

        resolver.Resolve(null).Should().BeNull();
    }

    private interface IFoo;

    private sealed class Foo : IFoo;
}
