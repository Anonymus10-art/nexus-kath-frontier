using System.Text;
using NexusKathFrontier.Launcher.Models;

namespace NexusKathFrontier.Launcher.Services;

public static class ServerListService
{
    public static void EnsureDefaultServer(LauncherConfig config)
    {
        AppPaths.EnsureDirectories();
        var path = Path.Combine(AppPaths.Game, "servers.dat");
        if (File.Exists(path)) return;

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);

        writer.Write((byte)10); // Root TAG_Compound
        WriteString(writer, string.Empty);

        writer.Write((byte)9); // TAG_List
        WriteString(writer, "servers");
        writer.Write((byte)10); // List elements are TAG_Compound
        WriteInt32BigEndian(writer, 1);

        WriteTagString(writer, "name", config.LauncherName);
        WriteTagString(writer, "ip", $"{config.ServerAddress}:{config.ServerPort}");
        WriteTagByte(writer, "acceptTextures", 1);
        WriteTagByte(writer, "hidden", 0);
        writer.Write((byte)0); // End server compound
        writer.Write((byte)0); // End root compound
    }

    private static void WriteTagString(BinaryWriter writer, string name, string value)
    {
        writer.Write((byte)8);
        WriteString(writer, name);
        WriteString(writer, value);
    }

    private static void WriteTagByte(BinaryWriter writer, string name, byte value)
    {
        writer.Write((byte)1);
        WriteString(writer, name);
        writer.Write(value);
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > ushort.MaxValue)
            throw new InvalidDataException("Cadena NBT demasiado larga.");
        writer.Write((byte)(bytes.Length >> 8));
        writer.Write((byte)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteInt32BigEndian(BinaryWriter writer, int value)
    {
        writer.Write((byte)(value >> 24));
        writer.Write((byte)(value >> 16));
        writer.Write((byte)(value >> 8));
        writer.Write((byte)value);
    }
}
