using SRVS.Application.Services;

namespace SRVS.Tests;

public class SyllabusFileNamingTests
{
    [Fact]
    public void BuildVersionedFileName_CreatesExpectedFormat()
    {
        var fileName = SyllabusFileNaming.BuildVersionedFileName("CS301", "First Sem 2026", 2, ".pdf");

        Assert.Equal("CS301_FirstSem2026_V2.pdf", fileName);
    }

    [Fact]
    public void NormalizeSegment_RemovesNonAlphanumericCharacters()
    {
        var segment = SyllabusFileNaming.NormalizeSegment("CPESD-3 / Lab#1");

        Assert.Equal("CPESD3Lab1", segment);
    }
}