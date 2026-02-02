using System.Text;

namespace Bex;

internal static class ExtraEncoding
{
    public static readonly Encoding ShiftJis;

    static ExtraEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ShiftJis = Encoding.GetEncoding("shift-jis");
    }
}
