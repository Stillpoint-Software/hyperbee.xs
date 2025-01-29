using System.Numerics;

namespace Hyperbee.XS.Extensions.Tests;

public class TestClass
{
    public int PropertyValue { get; set; }
    public int MethodValue() => PropertyValue;

    public TestClass PropertyThis => this;
    public TestClass MethodThis() => this;
    public TestClass this[string v] { get => this; }

    public int this[int i] { get => i; set => i = value; }
    public int this[int i, int j] { get => i + j; }

    public int AddNumbers( int x, int y ) => x + y;

    public static int StaticAddNumbers( int x, int y ) => x + y;

    public T GenericAdd<T>( T x, T y ) where T : INumber<T> => x + y;

    public TestClass( int value )
    {
        PropertyValue = value;
    }
}
