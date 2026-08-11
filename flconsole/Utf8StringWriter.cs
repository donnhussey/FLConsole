using System.IO;
using System.Text;

namespace flconsole;

internal sealed class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => new UTF8Encoding(false);
}
