using Backend.Util;

namespace Tests;

public class GatewayTests
{
    [Fact]
    public void TestArrayQueryFormat()
    {
        Assert.Equal("{1}, {2}, {3}", Util.GenerateStringArrayFormat(3, 1));
    }

    [Fact]
    public void TestArrayQueryFormat2()
    {
        Assert.Equal("{0}, {1}, {2}", Util.GenerateStringArrayFormat(3));
    }
}