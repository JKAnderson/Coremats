namespace Coremats;
public abstract class FileFormat
{
    protected static T ReadFile<T>(string path, Func<BinaryReaderEx, T> read)
    {
        using var fs = File.OpenRead(path);
        var br = new BinaryReaderEx(false, fs);
        return read(br);
    }

    protected static T ReadBytes<T>(byte[] bytes, Func<BinaryReaderEx, T> read)
    {
        var br = new BinaryReaderEx(false, bytes);
        return read(br);
    }

    protected static void WriteFile(string path, Action<BinaryWriterEx> write)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = File.Create(path);
        var bw = new BinaryWriterEx(false, fs);
        write(bw);
        bw.Finish();
    }

    protected static byte[] WriteBytes(Action<BinaryWriterEx> write)
    {
        var bw = new BinaryWriterEx(false);
        write(bw);
        return bw.FinishBytes();
    }
}
