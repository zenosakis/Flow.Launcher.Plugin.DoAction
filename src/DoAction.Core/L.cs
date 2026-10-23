using System.Globalization;

namespace DoAction.Core;

// English is the fallback for every language except Korean.
public static class L
{
    private static CultureInfo? culture;
    public static void SetCulture(CultureInfo value) => Volatile.Write(ref culture, value);
    public static string T(string english, string korean) =>
        (Volatile.Read(ref culture) ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName == "ko"
            ? korean : english;
}
