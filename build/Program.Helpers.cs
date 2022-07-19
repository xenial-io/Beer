using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using static SimpleExec.Command;

namespace Xenial.Build;

internal static partial class Program
{
    public static async Task EnsureTools()
        => await RunAsync("dotnet", "tool restore");

    public static string Tabify(string s)
    {
        s = s ?? string.Empty;
        return string.Join(
                       Environment.NewLine,
                       s.Split("\n").Select(s => $"\t{s}")
                   );
    }
}
