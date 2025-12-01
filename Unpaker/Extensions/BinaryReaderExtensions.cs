using System.Text;

namespace Unpaker.Extensions;

/// <summary>
/// Extension methods for reading pak file data
/// </summary>
public static class BinaryReaderExtensions
{
    /// <summary>
    /// Read a boolean as a single byte (0 = false, 1 = true)
    /// </summary>
    public static bool ReadPakBool(this BinaryReader reader)
    {
        byte value = reader.ReadByte();
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidBoolException(value),
        };
    }

    /// <summary>
    /// Read a 20-byte GUID/hash
    /// </summary>
    public static byte[] ReadGuid(this BinaryReader reader)
    {
        return reader.ReadBytes(20);
    }

    /// <summary>
    /// Read an array with a length prefix
    /// </summary>
    public static T[] ReadArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
    {
        uint length = reader.ReadUInt32();
        return reader.ReadArrayLen((int)length, readFunc);
    }

    /// <summary>
    /// Read an array with a specified length
    /// </summary>
    public static T[] ReadArrayLen<T>(this BinaryReader reader, int length, Func<BinaryReader, T> readFunc)
    {
        var result = new T[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = readFunc(reader);
        }
        return result;
    }

    /// <summary>
    /// Read a pak string (length-prefixed, null-terminated)
    /// </summary>
    public static string ReadPakString(this BinaryReader reader)
    {
        int length = reader.ReadInt32();

        if (length < 0)
        {
            // UTF-16 string
            int charCount = -length;
            var chars = new ushort[charCount];
            for (int i = 0; i < charCount; i++)
            {
                chars[i] = reader.ReadUInt16();
            }

            // Find null terminator
            int nullIndex = Array.IndexOf(chars, (ushort)0);
            if (nullIndex < 0) nullIndex = chars.Length;

            return Encoding.Unicode.GetString(
                chars.Take(nullIndex).SelectMany(c => BitConverter.GetBytes(c)).ToArray()
            );
        }
        else
        {
            // ASCII string
            var bytes = reader.ReadBytes(length);

            // Find null terminator
            int nullIndex = Array.IndexOf(bytes, (byte)0);
            if (nullIndex < 0) nullIndex = bytes.Length;

            return Encoding.UTF8.GetString(bytes, 0, nullIndex);
        }
    }

    /// <summary>
    /// Read a specified number of bytes
    /// </summary>
    public static byte[] ReadLen(this BinaryReader reader, int length)
    {
        return reader.ReadBytes(length);
    }
}

