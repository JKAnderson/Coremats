using System.Drawing;
using System.Numerics;
using System.Text;

namespace Bex;

public class BexWriter
{
    private readonly BinaryWriter _bw;
    private readonly Stack<long> _jumps;
    private readonly Dictionary<string, long> _reservations;

    public Stream Stream { get; }
    public long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }
    public long Length => Stream.Length;

    public bool BigEndian { get; set; }
    public bool VarintLong { get; set; }
    public int VarintSize => VarintLong ? 8 : 4;

    public BexWriter(Stream output, bool bigEndian)
    {
        ArgumentNullException.ThrowIfNull(output);

        Stream = output;
        BigEndian = bigEndian;
        _bw = new BinaryWriter(output);
        _jumps = [];
        _reservations = [];
    }

    public BexWriter(bool bigEndian) : this(new MemoryStream(), bigEndian) { }

    #region Seek
    public void JumpIn(long position)
    {
        _jumps.Push(Stream.Position);
        Stream.Position = position;
    }

    public void JumpOut()
    {
        Stream.Position = _jumps.Pop();
    }

    public void Align(int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);

        while (Stream.Position % alignment > 0)
            WriteByte(0);
    }

    public void AlignRelative(int alignment, long start)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);

        while ((Stream.Position - start) % alignment > 0)
            WriteByte(0);
    }
    #endregion

    #region Util
    private void WriteReversedBytes(byte[] bytes)
    {
        Array.Reverse(bytes);
        _bw.Write(bytes);
    }

    private void Reserve(string name, string typeName, int length)
    {
        name = $"{name}:{typeName}";
        if (_reservations.ContainsKey(name))
            throw new ArgumentException("Key already reserved: " + name);

        _reservations[name] = Stream.Position;
        for (int i = 0; i < length; i++)
            WriteByte(0xfe);
    }

    private long Fill(string name, string typeName)
    {
        name = $"{name}:{typeName}";
        if (!_reservations.TryGetValue(name, out long jump))
            throw new ArgumentException("Key is not reserved: " + name);

        _reservations.Remove(name);
        return jump;
    }

    public void Finish()
    {
        if (_reservations.Count > 0)
            throw new InvalidOperationException("Not all reservations filled: " + string.Join(", ", _reservations.Keys));

        _bw.Close();
    }

    public byte[] FinishBytes()
    {
        var ms = (MemoryStream)Stream;
        byte[] result = ms.ToArray();
        Finish();
        return result;
    }

    public void WritePattern(int length, byte pattern)
    {
        byte[] bytes = new byte[length];
        if (pattern != 0)
        {
            for (int i = 0; i < length; i++)
                bytes[i] = pattern;
        }
        WriteBytes(bytes);
    }
    #endregion

    #region Boolean
    public void WriteBoolean(bool value)
    {
        _bw.Write(value);
    }

    public void WriteBooleans(IList<bool> values)
    {
        foreach (bool value in values)
            WriteBoolean(value);
    }

    public void ReserveBoolean(string name)
    {
        Reserve(name, "Boolean", 1);
    }

    public void FillBoolean(string name, bool value)
    {
        JumpIn(Fill(name, "Boolean"));
        WriteBoolean(value);
        JumpOut();
    }
    #endregion

    #region SByte
    public void WriteSByte(sbyte value)
    {
        _bw.Write(value);
    }

    public void WriteSBytes(IList<sbyte> values)
    {
        foreach (sbyte value in values)
            WriteSByte(value);
    }

    public void ReserveSByte(string name)
    {
        Reserve(name, "SByte", 1);
    }

    public void FillSByte(string name, sbyte value)
    {
        JumpIn(Fill(name, "SByte"));
        WriteSByte(value);
        JumpOut();
    }
    #endregion

    #region Byte
    public void WriteByte(byte value)
    {
        _bw.Write(value);
    }

    public void WriteBytes(byte[] bytes)
    {
        _bw.Write(bytes);
    }

    public void WriteBytes(IList<byte> values)
    {
        foreach (byte value in values)
            WriteByte(value);
    }

    public void ReserveByte(string name)
    {
        Reserve(name, "Byte", 1);
    }

    public void FillByte(string name, byte value)
    {
        JumpIn(Fill(name, "Byte"));
        WriteByte(value);
        JumpOut();
    }
    #endregion

    #region Int16
    public void WriteInt16(short value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteInt16s(IList<short> values)
    {
        foreach (short value in values)
            WriteInt16(value);
    }

    public void ReserveInt16(string name)
    {
        Reserve(name, "Int16", 2);
    }

    public void FillInt16(string name, short value)
    {
        JumpIn(Fill(name, "Int16"));
        WriteInt16(value);
        JumpOut();
    }
    #endregion

    #region UInt16
    public void WriteUInt16(ushort value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteUInt16s(IList<ushort> values)
    {
        foreach (ushort value in values)
            WriteUInt16(value);
    }

    public void ReserveUInt16(string name)
    {
        Reserve(name, "UInt16", 2);
    }

    public void FillUInt16(string name, ushort value)
    {
        JumpIn(Fill(name, "UInt16"));
        WriteUInt16(value);
        JumpOut();
    }
    #endregion

    #region Int32
    public void WriteInt32(int value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteInt32s(IList<int> values)
    {
        foreach (int value in values)
            WriteInt32(value);
    }

    public void ReserveInt32(string name)
    {
        Reserve(name, "Int32", 4);
    }

    public void FillInt32(string name, int value)
    {
        JumpIn(Fill(name, "Int32"));
        WriteInt32(value);
        JumpOut();
    }
    #endregion

    #region UInt32
    public void WriteUInt32(uint value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteUInt32s(IList<uint> values)
    {
        foreach (uint value in values)
            WriteUInt32(value);
    }

    public void ReserveUInt32(string name)
    {
        Reserve(name, "UInt32", 4);
    }

    public void FillUInt32(string name, uint value)
    {
        JumpIn(Fill(name, "UInt32"));
        WriteUInt32(value);
        JumpOut();
    }
    #endregion

    #region Int64
    public void WriteInt64(long value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteInt64s(IList<long> values)
    {
        foreach (long value in values)
            WriteInt64(value);
    }

    public void ReserveInt64(string name)
    {
        Reserve(name, "Int64", 8);
    }

    public void FillInt64(string name, long value)
    {
        JumpIn(Fill(name, "Int64"));
        WriteInt64(value);
        JumpOut();
    }
    #endregion

    #region UInt64
    public void WriteUInt64(ulong value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteUInt64s(IList<ulong> values)
    {
        foreach (ulong value in values)
            WriteUInt64(value);
    }

    public void ReserveUInt64(string name)
    {
        Reserve(name, "UInt64", 8);
    }

    public void FillUInt64(string name, ulong value)
    {
        JumpIn(Fill(name, "UInt64"));
        WriteUInt64(value);
        JumpOut();
    }
    #endregion

    #region Varint
    public void WriteVarint(long value)
    {
        if (VarintLong)
            WriteInt64(value);
        else
            WriteInt32((int)value);
    }

    public void WriteVarints(IList<long> values)
    {
        foreach (long value in values)
        {
            if (VarintLong)
                WriteInt64(value);
            else
                WriteInt32((int)value);
        }
    }

    public void ReserveVarint(string name)
    {
        if (VarintLong)
            Reserve(name, "Varint64", 8);
        else
            Reserve(name, "Varint32", 4);
    }

    public void FillVarint(string name, long value)
    {
        if (VarintLong)
        {
            JumpIn(Fill(name, "Varint64"));
            WriteInt64(value);
            JumpOut();
        }
        else
        {
            JumpIn(Fill(name, "Varint32"));
            WriteInt32((int)value);
            JumpOut();
        }
    }
    #endregion

    #region Single
    public void WriteSingle(float value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteSingles(IList<float> values)
    {
        foreach (float value in values)
            WriteSingle(value);
    }

    public void ReserveSingle(string name)
    {
        Reserve(name, "Single", 4);
    }

    public void FillSingle(string name, float value)
    {
        JumpIn(Fill(name, "Single"));
        WriteSingle(value);
        JumpOut();
    }
    #endregion

    #region Double
    public void WriteDouble(double value)
    {
        if (BigEndian)
            WriteReversedBytes(BitConverter.GetBytes(value));
        else
            _bw.Write(value);
    }

    public void WriteDoubles(IList<double> values)
    {
        foreach (double value in values)
            WriteDouble(value);
    }

    public void ReserveDouble(string name)
    {
        Reserve(name, "Double", 8);
    }

    public void FillDouble(string name, double value)
    {
        JumpIn(Fill(name, "Double"));
        WriteDouble(value);
        JumpOut();
    }
    #endregion

    #region String
    private void WriteChars(string text, Encoding encoding, bool nullTerminate)
    {
        if (nullTerminate)
            text += '\0';
        byte[] bytes = encoding.GetBytes(text);
        _bw.Write(bytes);
    }

    public void WriteAscii(string text, bool nullTerminate = false)
    {
        WriteChars(text, Encoding.ASCII, nullTerminate);
    }

    public void WriteShiftJis(string text, bool nullTerminate = false)
    {
        WriteChars(text, ExtraEncoding.ShiftJis, nullTerminate);
    }

    public void WriteShiftJisFixed(string text, int size, byte padding = 0)
    {
        byte[] fixstr = new byte[size];
        for (int i = 0; i < size; i++)
            fixstr[i] = padding;

        byte[] bytes = ExtraEncoding.ShiftJis.GetBytes(text + '\0');
        Array.Copy(bytes, fixstr, Math.Min(size, bytes.Length));
        _bw.Write(fixstr);
    }

    public void WriteUtf16(string text, bool nullTerminate = false)
    {
        if (BigEndian)
            WriteChars(text, Encoding.BigEndianUnicode, nullTerminate);
        else
            WriteChars(text, Encoding.Unicode, nullTerminate);
    }

    public void WriteUtf16Fixed(string text, int size, byte padding = 0)
    {
        byte[] fixstr = new byte[size];
        for (int i = 0; i < size; i++)
            fixstr[i] = padding;

        byte[] bytes;
        if (BigEndian)
            bytes = Encoding.BigEndianUnicode.GetBytes(text + '\0');
        else
            bytes = Encoding.Unicode.GetBytes(text + '\0');
        Array.Copy(bytes, fixstr, Math.Min(size, bytes.Length));
        _bw.Write(fixstr);
    }
    #endregion

    #region Other
    public void WriteVector2(Vector2 vector)
    {
        WriteSingle(vector.X);
        WriteSingle(vector.Y);
    }

    public void WriteVector3(Vector3 vector)
    {
        WriteSingle(vector.X);
        WriteSingle(vector.Y);
        WriteSingle(vector.Z);
    }

    public void WriteVector4(Vector4 vector)
    {
        WriteSingle(vector.X);
        WriteSingle(vector.Y);
        WriteSingle(vector.Z);
        WriteSingle(vector.W);
    }

    public void WriteARGB(Color color)
    {
        _bw.Write(color.A);
        _bw.Write(color.R);
        _bw.Write(color.G);
        _bw.Write(color.B);
    }

    public void WriteABGR(Color color)
    {
        _bw.Write(color.A);
        _bw.Write(color.B);
        _bw.Write(color.G);
        _bw.Write(color.R);
    }

    public void WriteRGBA(Color color)
    {
        _bw.Write(color.R);
        _bw.Write(color.G);
        _bw.Write(color.B);
        _bw.Write(color.A);
    }

    public void WriteBGRA(Color color)
    {
        _bw.Write(color.B);
        _bw.Write(color.G);
        _bw.Write(color.R);
        _bw.Write(color.A);
    }
    #endregion
}
