namespace SpeakingCoach.Api.Services;

/// <summary>Ulanish satriga xavfsiz standart sozlamalar qo'shadi (berilganlarini o'zgartirmaydi).</summary>
public static class DbConnection
{
    /// <summary>
    /// "GSS Encryption Mode=Disable": Neon Kerberos ishlatmaydi, Docker obrazida esa
    /// libgssapi yo'q — busiz Npgsql har ulanishda uni yuklashga urinib, logga
    /// ogohlantirish yozadi va ulanish sekinlashadi. URL ko'rinishidagi satrlar tegilmaydi.
    /// </summary>
    public static string Normalize(string connectionString)
    {
        var cs = connectionString.Trim();
        if (cs.Length == 0 || cs.Contains("://", StringComparison.Ordinal)) return cs;
        if (cs.Contains("GSS Encryption Mode", StringComparison.OrdinalIgnoreCase)
            || cs.Contains("GssEncryptionMode", StringComparison.OrdinalIgnoreCase)) return cs;
        return cs.TrimEnd(';') + ";GSS Encryption Mode=Disable";
    }
}
