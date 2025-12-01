using System.Text;

namespace Unpaker.Extensions;

/// <summary>
/// Extension methods for writing pak file data
/// </summary>
public static class BinaryWriterExtensions
{
    /// <summary>
    /// Write a boolean as a single byte (0 = false, 1 = true)
    /// </summary>
    public static void WritePakBool(this BinaryWriter writer, bool value)
    {
        writer.Write((byte)(value ? 1 : 0));
    }

    /// <summary>
    /// Write a pak string (length-prefixed, null-terminated)
    /// </summary>
    public static void WritePakString(this BinaryWriter writer, string value)
    {
        if (string.IsNullOrEmpty(value) || value.All(c => c < 128))
        {
            // ASCII string
            writer.Write((uint)(value.Length + 1));
            writer.Write(Encoding.UTF8.GetBytes(value));
            writer.Write((byte)0); // null terminator
        }
        else
        {
            // UTF-16 string
            var chars = value.ToCharArray();
            writer.Write(-(chars.Length + 1));
            foreach (char c in chars)
            {
                writer.Write((ushort)c);
            }
            writer.Write((ushort)0); // null terminator
        }
    }
}

