using Xunit;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Assert.True(true); // Dies ist ein erfolgreicher Testfall
    }

    [Theory]
    [InlineData(2, 3, 5)] // Beispiel einer parametrisierten Eingabe
    [InlineData(1, 1, 2)]
    public void AddNumbers_TheoryTest(int a, int b, int expected)
    {
        int result = a + b;
        Assert.Equal(expected, result); // Überprüfe, ob die Summe korrekt ist
    }
}