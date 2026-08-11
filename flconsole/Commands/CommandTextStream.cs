using System.IO;
using System.Text;

namespace flconsole.Commands;

internal static class CommandTextStream
{
    public static Stream Create(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}