using System.Reflection;
using System.Text;

namespace AnalogAgenda.EmailSender.Resources;

public class ResourceLoader
{
    public static string GetText(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream(resourceName) 
                                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found in the assembly.");

        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
