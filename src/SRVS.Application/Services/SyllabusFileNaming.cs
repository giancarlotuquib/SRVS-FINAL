namespace SRVS.Application.Services;

public static class SyllabusFileNaming
{
    public static string BuildVersionedFileName(string courseCode, string semester, int versionNumber, string extension)
    {
        var safeCourseCode = NormalizeSegment(courseCode);
        var safeSemester = NormalizeSegment(semester);
        var safeExtension = extension.StartsWith('.') ? extension : $".{extension}";

        return $"{safeCourseCode}_{safeSemester}_V{versionNumber}{safeExtension}";
    }

    public static string NormalizeSegment(string value)
    {
        var cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "File" : cleaned;
    }
}