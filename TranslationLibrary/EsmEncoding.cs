using System;
using System.Text;

namespace TranslationLibrary;

public static class EsmEncoding
{
    public const string CentralOrWesternEuropean = "win1250";
    public const string Cyrillic = "win1251";
    public const string English = "win1252";
    
    static EsmEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    
    public static Encoding ToSysEncoding(string encoding)
    {
        switch (encoding)
        {
            case CentralOrWesternEuropean:
                return Encoding.GetEncoding("windows-1250");
            case Cyrillic:
                return Encoding.GetEncoding("windows-1251");
            case English:
                return Encoding.GetEncoding("windows-1252");
            default:
                throw new Exception($"Unsupported encoding '{encoding}'");
        }
    }
}
