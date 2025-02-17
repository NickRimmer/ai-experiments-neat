namespace Experiments;

public class StringExperiments
{
    [TestCase("Input")]
    [TestCase("MultipleWords")]
    [TestCase("firstSmall")]
    [TestCase("allsmall")]
    public void PrintAbbreviation(string src)
    {
        var result = src.GetAbbreviation();
        Console.WriteLine(result);
    }
}
