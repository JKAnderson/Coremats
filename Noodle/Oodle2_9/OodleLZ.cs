using System.Runtime.InteropServices;

namespace Noodle.Oodle2_9;

public class OodleLZ
{
    public enum OodleLZ_Verbosity : int
    {
        OodleLZ_Verbosity_None = 0,
        OodleLZ_Verbosity_Minimal = 1,
        OodleLZ_Verbosity_Some = 2,
        OodleLZ_Verbosity_Lots = 3,
        OodleLZ_Verbosity_Force32 = 0x40000000
    }

    public enum OodleLZ_Compressor : int
    {
        OodleLZ_Compressor_Invalid = -1,
        OodleLZ_Compressor_None = 3,

        OodleLZ_Compressor_Kraken = 8,
        OodleLZ_Compressor_Leviathan = 13,
        OodleLZ_Compressor_Mermaid = 9,
        OodleLZ_Compressor_Selkie = 11,
        OodleLZ_Compressor_Hydra = 12,

        //OodleLZ_Compressor_BitKnit = 10,
        //OodleLZ_Compressor_LZB16 = 4,
        //OodleLZ_Compressor_LZNA = 7,
        //OodleLZ_Compressor_LZH = 0,
        //OodleLZ_Compressor_LZHLW = 1,
        //OodleLZ_Compressor_LZNIB = 2,
        //OodleLZ_Compressor_LZBLW = 5,
        //OodleLZ_Compressor_LZA = 6,

        OodleLZ_Compressor_Count = 14,
        OodleLZ_Compressor_Force32 = 0x40000000
    }

    public enum OodleLZ_CheckCRC : int
    {
        OodleLZ_CheckCRC_No = 0,
        OodleLZ_CheckCRC_Yes = 1,
        OodleLZ_CheckCRC_Force32 = 0x40000000
    }

    public enum OodleLZ_Profile : int
    {
        OodleLZ_Profile_Main = 0,
        OodleLZ_Profile_Reduced = 1,
        OodleLZ_Profile_Force32 = 0x40000000
    }

    public enum OodleLZ_Jobify : int
    {
        OodleLZ_Jobify_Default = 0,
        OodleLZ_Jobify_Disable = 1,
        OodleLZ_Jobify_Normal = 2,
        OodleLZ_Jobify_Aggressive = 3,
        OodleLZ_Jobify_Count = 4,

        OodleLZ_Jobify_Force32 = 0x40000000,
    }

    public enum OodleLZ_CompressionLevel : int
    {
        OodleLZ_CompressionLevel_None = 0,
        OodleLZ_CompressionLevel_SuperFast = 1,
        OodleLZ_CompressionLevel_VeryFast = 2,
        OodleLZ_CompressionLevel_Fast = 3,
        OodleLZ_CompressionLevel_Normal = 4,

        OodleLZ_CompressionLevel_Optimal1 = 5,
        OodleLZ_CompressionLevel_Optimal2 = 6,
        OodleLZ_CompressionLevel_Optimal3 = 7,
        OodleLZ_CompressionLevel_Optimal4 = 8,
        OodleLZ_CompressionLevel_Optimal5 = 9,

        OodleLZ_CompressionLevel_HyperFast1 = -1,
        OodleLZ_CompressionLevel_HyperFast2 = -2,
        OodleLZ_CompressionLevel_HyperFast3 = -3,
        OodleLZ_CompressionLevel_HyperFast4 = -4,

        OodleLZ_CompressionLevel_HyperFast = OodleLZ_CompressionLevel_HyperFast1,
        OodleLZ_CompressionLevel_Optimal = OodleLZ_CompressionLevel_Optimal2,
        OodleLZ_CompressionLevel_Max = OodleLZ_CompressionLevel_Optimal5,
        OodleLZ_CompressionLevel_Min = OodleLZ_CompressionLevel_HyperFast4,

        OodleLZ_CompressionLevel_Force32 = 0x40000000,
        OodleLZ_CompressionLevel_Invalid = OodleLZ_CompressionLevel_Force32
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OodleLZ_CompressOptions
    {
        public uint unused_was_verbosity;
        public int minMatchLen;
        [MarshalAs(UnmanagedType.Bool)]
        public bool seekChunkReset;
        public int seekChunkLen;
        public OodleLZ_Profile profile;
        public int dictionarySize;
        public int spaceSpeedTradeoffBytes;
        public int unused_was_maxHuffmansPerChunk;
        [MarshalAs(UnmanagedType.Bool)]
        public bool sendQuantumCRCs;
        public int maxLocalDictionarySize;
        [MarshalAs(UnmanagedType.Bool)]
        public bool makeLongRangeMatcher;
        public int matchTableSizeLog2;

        public OodleLZ_Jobify jobify;
        public IntPtr jobifyUserPtr;

        public int farMatchMinLen;
        public int farMatchOffsetLog2;

        public uint reserved0;
        public uint reserved1;
        public uint reserved2;
        public uint reserved3;
    }

    public enum OodleLZ_Decode_ThreadPhase : int
    {
        OodleLZ_Decode_ThreadPhase1 = 1,
        OodleLZ_Decode_ThreadPhase2 = 2,
        OodleLZ_Decode_ThreadPhaseAll = 3,
        OodleLZ_Decode_Unthreaded = OodleLZ_Decode_ThreadPhaseAll
    }

    public enum OodleLZ_FuzzSafe : int
    {
        OodleLZ_FuzzSafe_No = 0,
        OodleLZ_FuzzSafe_Yes = 1
    }

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern nint OodleLZ_Compress(
        OodleLZ_Compressor compressor,
        [MarshalAs(UnmanagedType.LPArray)]
        byte[] rawBuf,
        nint rawLen,
        [MarshalAs(UnmanagedType.LPArray)]
        byte[] compBuf,
        OodleLZ_CompressionLevel level,
        IntPtr pOptions = 0,
        IntPtr dictionaryBase = 0,
        IntPtr lrm = 0,
        IntPtr scratchMem = 0,
        nint scratchSize = 0
        );

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern nint OodleLZ_Decompress(
        [MarshalAs(UnmanagedType.LPArray)]
        byte[] compBuf,
        nint compBufSize,
        [MarshalAs(UnmanagedType.LPArray)]
        byte[] rawBuf,
        nint rawLen,
        OodleLZ_FuzzSafe fuzzSafe = OodleLZ_FuzzSafe.OodleLZ_FuzzSafe_Yes,
        OodleLZ_CheckCRC checkCRC = OodleLZ_CheckCRC.OodleLZ_CheckCRC_No,
        OodleLZ_Verbosity verbosity = OodleLZ_Verbosity.OodleLZ_Verbosity_None,
        IntPtr decBufBase = 0,
        nint decBufSize = 0,
        IntPtr fpCallback = 0,
        IntPtr callbackUserData = 0,
        IntPtr decoderMemory = 0,
        nint decoderMemorySize = 0,
        OodleLZ_Decode_ThreadPhase threadPhase = OodleLZ_Decode_ThreadPhase.OodleLZ_Decode_Unthreaded
        );

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr OodleLZ_CompressOptions_GetDefault(
        OodleLZ_Compressor compressor = OodleLZ_Compressor.OodleLZ_Compressor_Invalid,
        OodleLZ_CompressionLevel lzLevel = OodleLZ_CompressionLevel.OodleLZ_CompressionLevel_Normal
        );

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern nint OodleLZ_GetCompressedBufferSizeNeeded(
        OodleLZ_Compressor compressor,
        nint rawSize
        );

    [DllImport("oo2core_9_win64.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern nint OodleLZ_GetDecodeBufferSize(
        OodleLZ_Compressor compressor,
        nint rawSize,
        [MarshalAs(UnmanagedType.Bool)]
        bool corruptionPossible
        );
}
