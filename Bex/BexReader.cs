using System.Buffers.Binary;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Bex;

public class BexReader
{
    /// <summary>
    /// Size of stack-allocated buffers for string reading, in bytes; must be even and at least 2.
    /// </summary>
    private const int STRING_BUFFER_STACK_LIMIT = 128;

    private readonly byte[]? _raw = null;
    private readonly Stack<long> _jumps;

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

    public BexReader(Stream input, bool bigEndian)
    {
        ArgumentNullException.ThrowIfNull(input);

        Stream = input;
        BigEndian = bigEndian;
        _jumps = [];
    }

    public BexReader(byte[] input, bool bigEndian) : this(new MemoryStream(input), bigEndian)
    {
        _raw = input;
    }

    #region Seek
    public void Skip(int length)
    {
        Stream.Position += length;
    }

    public void Align(int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);

        long offset = Stream.Position % alignment;
        if (offset != 0)
            Stream.Position += alignment - offset;
    }

    public void AlignRelative(int alignment, long start)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);

        long offset = (Stream.Position - start) % alignment;
        if (offset != 0)
            Stream.Position += alignment - offset;
    }

    public void JumpIn(long position)
    {
        _jumps.Push(Stream.Position);
        Stream.Position = position;
    }

    public void JumpOut()
    {
        Stream.Position = _jumps.Pop();
    }
    #endregion

    #region Utility
    protected long PeekStart(long position)
    {
        long pos = Stream.Position;
        Stream.Position = position;
        return pos;
    }

    protected T PeekFinish<T>(long pos, T value)
    {
        Stream.Position = pos;
        return value;
    }

    protected T AssertValue<T>(T value, ReadOnlySpan<T> options) where T : IEquatable<T>
    {
        if (!options.Contains(value))
        {
            string valueFormat = value switch
            {
                byte or sbyte
                or ushort or short
                or uint or int
                or ulong or long => "{0} (0x{0:X})",
                string => "\"{0}\"",
                _ => "{0}",
            };
            string valueStr = string.Format(valueFormat, value);
            string optionsStr = string.Join(", ", options.ToArray().Select(o => string.Format(valueFormat, o)));
            throw new InvalidDataException($"Read {typeof(T).Name}: {valueStr} | Expected: {optionsStr} | Ending position: 0x{Position:X}");
        }

        return value;
    }

    public void AssertPattern(int length, byte pattern)
    {
        byte[] bytes = ReadBytes(length);
        for (int i = 0; i < length; i++)
        {
            if (bytes[i] != pattern)
                throw new InvalidDataException($"Expected {length} 0x{pattern:X2}, got 0x{bytes[i]:X2} at position {i}");
        }
    }
    #endregion

    #region Byte
    protected ReadOnlySpan<byte> ReadDirect(Span<byte> buffer)
    {
        if (_raw != null)
        {
            int pos = (int)Stream.Position;
            int next = pos + buffer.Length;
            if (next > _raw.Length)
                throw new EndOfStreamException();

            buffer = _raw.AsSpan(pos, buffer.Length);
            Stream.Position = next;
            return buffer;
        }
        else
        {
            Stream.ReadExactly(buffer);
            return buffer;
        }
    }

    protected ReadOnlySpan<byte> ReadDirect(int length)
    {
        if (_raw != null)
        {
            int pos = (int)Stream.Position;
            int next = pos + length;
            if (next > _raw.Length)
                throw new EndOfStreamException();

            var buffer = _raw.AsSpan(pos, length);
            Stream.Position = next;
            return buffer;
        }
        else
        {
            var buffer = new byte[length];
            Stream.ReadExactly(buffer);
            return buffer;
        }
    }

    public byte ReadByte()
    {
        int value = Stream.ReadByte();
        if (value == -1)
            throw new EndOfStreamException();
        return (byte)value;
    }

    public byte[] ReadBytes(int count)
    {
        var value = new byte[count];
        Stream.ReadExactly(value);
        return value;
    }

    public void ReadBytes(Span<byte> buffer)
    {
        Stream.ReadExactly(buffer);
    }

    public byte PeekByte(long position)
    {
        return PeekFinish(PeekStart(position), ReadByte());
    }

    public byte[] PeekBytes(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadBytes(count));
    }

    public void PeekBytes(long position, Span<byte> buffer)
    {
        long pos = PeekStart(position);
        ReadBytes(buffer);
        Stream.Position = pos;
    }

    public byte AssertByte(params ReadOnlySpan<byte> options)
    {
        return AssertValue(ReadByte(), options);
    }
    #endregion

    #region Boolean
    public bool ReadBoolean()
    {
        byte b = ReadByte();
        return b switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException($"ReadBoolean encountered non-boolean value: 0x{b:X2}")
        };
    }

    public bool[] ReadBooleans(int count)
    {
        var value = new bool[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadBoolean();
        return value;
    }

    public bool PeekBoolean(long position)
    {
        return PeekFinish(PeekStart(position), ReadBoolean());
    }

    public bool[] PeekBooleans(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadBooleans(count));
    }

    public bool AssertBoolean(bool option)
    {
        return AssertValue(ReadBoolean(), [option]);
    }
    #endregion

    #region SByte
    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    public sbyte[] ReadSBytes(int count)
    {
        var value = new sbyte[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadSByte();
        return value;
    }

    public sbyte PeekSByte(long position)
    {
        return PeekFinish(PeekStart(position), ReadSByte());
    }

    public sbyte[] PeekSBytes(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadSBytes(count));
    }

    public sbyte AssertSByte(params ReadOnlySpan<sbyte> options)
    {
        return AssertValue(ReadSByte(), options);
    }
    #endregion

    #region UInt16
    public ushort ReadUInt16()
    {
        var buffer = ReadDirect(stackalloc byte[2]);
        if (BigEndian)
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        else
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
    }

    public ushort[] ReadUInt16s(int count)
    {
        var value = new ushort[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadUInt16();
        return value;
    }

    public ushort PeekUInt16(long position)
    {
        return PeekFinish(PeekStart(position), ReadUInt16());
    }

    public ushort[] PeekUInt16s(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadUInt16s(count));
    }

    public ushort AssertUInt16(params ReadOnlySpan<ushort> options)
    {
        return AssertValue(ReadUInt16(), options);
    }
    #endregion

    #region Int16
    public short ReadInt16()
    {
        var buffer = ReadDirect(stackalloc byte[2]);
        if (BigEndian)
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        else
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
    }

    public short[] ReadInt16s(int count)
    {
        var value = new short[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadInt16();
        return value;
    }

    public short PeekInt16(long position)
    {
        return PeekFinish(PeekStart(position), ReadInt16());
    }

    public short[] PeekInt16s(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadInt16s(count));
    }

    public short AssertInt16(params ReadOnlySpan<short> options)
    {
        return AssertValue(ReadInt16(), options);
    }
    #endregion

    #region UInt32
    public uint ReadUInt32()
    {
        var buffer = ReadDirect(stackalloc byte[4]);
        if (BigEndian)
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        else
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    public uint[] ReadUInt32s(int count)
    {
        var value = new uint[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadUInt32();
        return value;
    }

    public uint PeekUInt32(long position)
    {
        return PeekFinish(PeekStart(position), ReadUInt32());
    }

    public uint[] PeekUInt32s(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadUInt32s(count));
    }

    public uint AssertUInt32(params ReadOnlySpan<uint> options)
    {
        return AssertValue(ReadUInt32(), options);
    }
    #endregion

    #region Int32
    public int ReadInt32()
    {
        var buffer = ReadDirect(stackalloc byte[4]);
        if (BigEndian)
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        else
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    public int[] ReadInt32s(int count)
    {
        int[] value = new int[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadInt32();
        return value;
    }

    public int PeekInt32(long position)
    {
        return PeekFinish(PeekStart(position), ReadInt32());
    }

    public int[] PeekInt32s(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadInt32s(count));
    }

    public int AssertInt32(params ReadOnlySpan<int> options)
    {
        return AssertValue(ReadInt32(), options);
    }
    #endregion

    #region UInt64
    public ulong ReadUInt64()
    {
        var buffer = ReadDirect(stackalloc byte[8]);
        if (BigEndian)
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
        else
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
    }

    public ulong[] ReadUInt64s(int count)
    {
        var value = new ulong[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadUInt64();
        return value;
    }

    public ulong PeekUInt64(long position)
    {
        return PeekFinish(PeekStart(position), ReadUInt64());
    }

    public ulong[] PeekUInt64s(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadUInt64s(count));
    }

    public ulong AssertUInt64(params ReadOnlySpan<ulong> options)
    {
        return AssertValue(ReadUInt64(), options);
    }
    #endregion

    #region Int64
    public long ReadInt64()
    {
        var buffer = ReadDirect(stackalloc byte[8]);
        if (BigEndian)
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
        else
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
    }

    public long[] ReadInt64s(int count)
    {
        var value = new long[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadInt64();
        return value;
    }

    public long PeekInt64(long position)
    {
        return PeekFinish(PeekStart(position), ReadInt64());
    }

    public long[] PeekInt64s(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadInt64s(count));
    }

    public long AssertInt64(params ReadOnlySpan<long> options)
    {
        return AssertValue(ReadInt64(), options);
    }
    #endregion

    #region Varint
    public long ReadVarint()
    {
        if (VarintLong)
            return ReadInt64();
        else
            return ReadInt32();
    }

    public long[] ReadVarints(int count)
    {
        var value = new long[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadVarint();
        return value;
    }

    public long PeekVarint(long position)
    {
        return PeekFinish(PeekStart(position), ReadVarint());
    }

    public long[] PeekVarints(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadVarints(count));
    }

    public long AssertVarint(params ReadOnlySpan<long> options)
    {
        return AssertValue(ReadVarint(), options);
    }
    #endregion

    #region Single
    public float ReadSingle()
    {
        var buffer = ReadDirect(stackalloc byte[4]);
        if (BigEndian)
            return BinaryPrimitives.ReadSingleBigEndian(buffer);
        else
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
    }

    public float[] ReadSingles(int count)
    {
        var value = new float[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadSingle();
        return value;
    }

    public float PeekSingle(long position)
    {
        return PeekFinish(PeekStart(position), ReadSingle());
    }

    public float[] PeekSingles(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadSingles(count));
    }

    public float AssertSingle(params ReadOnlySpan<float> options)
    {
        return AssertValue(ReadSingle(), options);
    }
    #endregion

    #region Double
    public double ReadDouble()
    {
        var buffer = ReadDirect(stackalloc byte[8]);
        if (BigEndian)
            return BinaryPrimitives.ReadDoubleBigEndian(buffer);
        else
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
    }

    public double[] ReadDoubles(int count)
    {
        var value = new double[count];
        for (int i = 0; i < count; i++)
            value[i] = ReadDouble();
        return value;
    }

    public double PeekDouble(long position)
    {
        return PeekFinish(PeekStart(position), ReadDouble());
    }

    public double[] PeekDoubles(long position, int count)
    {
        return PeekFinish(PeekStart(position), ReadDoubles(count));
    }

    public double AssertDouble(params ReadOnlySpan<double> options)
    {
        return AssertValue(ReadDouble(), options);
    }
    #endregion

    #region Enum
    private static TEnum AssertEnum<TValue, TEnum>(TValue value) where TValue : IBinaryInteger<TValue> where TEnum : struct, Enum
    {
        TEnum enumValue = (TEnum)(object)value;
        if (!Enum.IsDefined(enumValue))
            throw new InvalidDataException($"Read value not present in enum: {value} (0x{value:X})");

        return enumValue;
    }

    public TEnum ReadEnum8<TEnum>() where TEnum : struct, Enum
    {
        return AssertEnum<byte, TEnum>(ReadByte());
    }

    public TEnum PeekEnum8<TEnum>(long position) where TEnum : struct, Enum
    {
        return PeekFinish(PeekStart(position), ReadEnum8<TEnum>());
    }

    public TEnum ReadEnum16<TEnum>() where TEnum : struct, Enum
    {
        return AssertEnum<ushort, TEnum>(ReadUInt16());
    }

    public TEnum PeekEnum16<TEnum>(long position) where TEnum : struct, Enum
    {
        return PeekFinish(PeekStart(position), ReadEnum16<TEnum>());
    }

    public TEnum ReadEnum32<TEnum>() where TEnum : struct, Enum
    {
        return AssertEnum<uint, TEnum>(ReadUInt32());
    }

    public TEnum PeekEnum32<TEnum>(long position) where TEnum : struct, Enum
    {
        return PeekFinish(PeekStart(position), ReadEnum32<TEnum>());
    }

    public TEnum ReadEnum64<TEnum>() where TEnum : struct, Enum
    {
        return AssertEnum<ulong, TEnum>(ReadUInt64());
    }

    public TEnum PeekEnum64<TEnum>(long position) where TEnum : struct, Enum
    {
        return PeekFinish(PeekStart(position), ReadEnum64<TEnum>());
    }
    #endregion

    #region String
    protected string ReadString(Encoding encoding)
    {
        Span<byte> buffer = stackalloc byte[STRING_BUFFER_STACK_LIMIT];
        int length = 0;

        byte unit = ReadByte();
        while (unit != 0)
        {
            if (length >= buffer.Length)
            {
                var resize = new byte[buffer.Length * 2];
                buffer.CopyTo(resize);
                buffer = resize;
            }
            buffer[length] = unit;
            length++;
            unit = ReadByte();
        }
        return encoding.GetString(buffer[..length]);
    }

    protected string ReadString(Encoding encoding, int length)
    {
        var buffer = ReadDirect(length);
        return encoding.GetString(buffer);
    }

    protected string ReadStringFixed(Encoding encoding, int length)
    {
        var buffer = ReadDirect(length);
        int nullIndex = buffer.IndexOf((byte)0);
        return encoding.GetString(nullIndex == -1 ? buffer : buffer[..nullIndex]);
    }

    protected string PeekString(long position, Encoding encoding)
    {
        return PeekFinish(PeekStart(position), ReadString(encoding));
    }

    protected string PeekString(long position, Encoding encoding, int length)
    {
        return PeekFinish(PeekStart(position), ReadString(encoding, length));
    }

    protected string AssertString(Encoding encoding, params ReadOnlySpan<string> values)
    {
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);

        int length = encoding.GetByteCount(values[0]);
        return AssertValue(ReadString(encoding, length), values);
    }

    public string ReadAscii() => ReadString(Encoding.ASCII);
    public string ReadAscii(int length) => ReadString(Encoding.ASCII, length);
    public string ReadAsciiFixed(int length) => ReadStringFixed(Encoding.ASCII, length);
    public string PeekAscii(long position) => PeekString(position, Encoding.ASCII);
    public string PeekAscii(long position, int length) => PeekString(position, Encoding.ASCII, length);
    public string AssertAscii(params ReadOnlySpan<string> values) => AssertString(Encoding.ASCII, values);

    public string ReadShiftJis() => ReadString(ExtraEncoding.ShiftJis);
    public string ReadShiftJis(int length) => ReadString(ExtraEncoding.ShiftJis, length);
    public string ReadShiftJisFixed(int length) => ReadStringFixed(ExtraEncoding.ShiftJis, length);
    public string PeekShiftJis(long position) => PeekString(position, ExtraEncoding.ShiftJis);
    public string PeekShiftJis(long position, int length) => PeekString(position, ExtraEncoding.ShiftJis, length);
    public string AssertShiftJis(params ReadOnlySpan<string> values) => AssertString(ExtraEncoding.ShiftJis, values);

    protected string ReadWString(Encoding encoding)
    {
        Span<byte> buffer = stackalloc byte[STRING_BUFFER_STACK_LIMIT];
        int length = 0;

        Span<byte> span = stackalloc byte[2];
        var unit = ReadDirect(span);
        while (unit.ContainsAnyExcept((byte)0))
        {
            if (length >= buffer.Length)
            {
                var resize = new byte[buffer.Length * 2];
                buffer.CopyTo(resize);
                buffer = resize;
            }
            unit.CopyTo(buffer[length..]);
            length += 2;
            unit = ReadDirect(span);
        }
        return encoding.GetString(buffer[..length]);
    }

    protected string ReadWString(Encoding encoding, int length)
    {
        if (length % 2 != 0)
            throw new ArgumentException("Length must be a multiple of 2.", nameof(length));

        var buffer = ReadDirect(length);
        return encoding.GetString(buffer);
    }

    protected string ReadWStringFixed(Encoding encoding, int length)
    {
        if (length % 2 != 0)
            throw new ArgumentException("Length must be a multiple of 2.", nameof(length));

        var buffer = ReadDirect(length);
        var units = MemoryMarshal.Cast<byte, ushort>(buffer);
        int nullIndex = units.IndexOf((ushort)0);
        return encoding.GetString(nullIndex == -1 ? buffer : buffer[..(nullIndex * 2)]);
    }

    protected string PeekWString(long position, Encoding encoding)
    {
        return PeekFinish(PeekStart(position), ReadWString(encoding));
    }

    protected string PeekWString(long position, Encoding encoding, int length)
    {
        return PeekFinish(PeekStart(position), ReadWString(encoding, length));
    }

    protected string AssertWString(Encoding encoding, params ReadOnlySpan<string> values)
    {
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);

        int length = encoding.GetByteCount(values[0]);
        return AssertValue(ReadWString(encoding, length), values);
    }

    private Encoding Utf16Encoding => BigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
    public string ReadUtf16() => ReadWString(Utf16Encoding);
    public string ReadUtf16(int length) => ReadWString(Utf16Encoding, length);
    public string ReadUtf16Fixed(int length) => ReadWStringFixed(Utf16Encoding, length);
    public string PeekUtf16(long position) => PeekWString(position, Utf16Encoding);
    public string PeekUtf16(long position, int length) => PeekWString(position, Utf16Encoding, length);
    public string AssertUtf16(params ReadOnlySpan<string> values) => AssertWString(Utf16Encoding, values);
    #endregion

    #region Other
    public Vector2 ReadVector2()
    {
        float x = ReadSingle();
        float y = ReadSingle();
        return new Vector2(x, y);
    }

    public Vector3 ReadVector3()
    {
        float x = ReadSingle();
        float y = ReadSingle();
        float z = ReadSingle();
        return new Vector3(x, y, z);
    }

    public Vector4 ReadVector4()
    {
        float x = ReadSingle();
        float y = ReadSingle();
        float z = ReadSingle();
        float w = ReadSingle();
        return new Vector4(x, y, z, w);
    }

    public Color ReadARGB()
    {
        byte a = ReadByte();
        byte r = ReadByte();
        byte g = ReadByte();
        byte b = ReadByte();
        return Color.FromArgb(a, r, g, b);
    }

    public Color ReadABGR()
    {
        byte a = ReadByte();
        byte b = ReadByte();
        byte g = ReadByte();
        byte r = ReadByte();
        return Color.FromArgb(a, r, g, b);
    }

    public Color ReadRGBA()
    {
        byte r = ReadByte();
        byte g = ReadByte();
        byte b = ReadByte();
        byte a = ReadByte();
        return Color.FromArgb(a, r, g, b);
    }

    public Color ReadBGRA()
    {
        byte b = ReadByte();
        byte g = ReadByte();
        byte r = ReadByte();
        byte a = ReadByte();
        return Color.FromArgb(a, r, g, b);
    }
    #endregion
}
