using System;
using System.Runtime.InteropServices;

namespace TranslationLibrary;

public class Imports
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TranslationRecordInfo
    {
        public IntPtr Pointer;
        public IntPtr SourcePtr;
        public IntPtr TargetPtr;
        public IntPtr ContextNamePtr;
        public IntPtr ContextIdPtr;
        public IntPtr MetaPtr;
        public int Index;
        public int Type;
        public int MaxLength;

        public string Source => Marshal.PtrToStringUTF8(SourcePtr) ?? "";
        public string Target => Marshal.PtrToStringUTF8(TargetPtr) ?? "";
        public string ContextName => Marshal.PtrToStringUTF8(ContextNamePtr) ?? "";
        public string ContextId => Marshal.PtrToStringUTF8(ContextIdPtr) ?? "";
        public string Meta => Marshal.PtrToStringUTF8(MetaPtr) ?? "";
    }

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Translation_GetTexts([MarshalAs(UnmanagedType.LPWStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string encoding);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Translation_Save(IntPtr container, [MarshalAs(UnmanagedType.LPWStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string encoding);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Struct)]
    public static extern TranslationRecordInfo TranslationState_GetNextRecordInfo(IntPtr container);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TranslationState_Reset(IntPtr container);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TranslationState_Dispose(IntPtr container);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstUtf8StringMarshaller))]
    public static extern string TranslationRecord_GetSource(IntPtr record);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstUtf8StringMarshaller))]
    public static extern string TranslationRecord_GetTarget(IntPtr record);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstUtf8StringMarshaller))]
    public static extern string TranslationRecord_GetContextName(IntPtr record);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstUtf8StringMarshaller))]
    public static extern string TranslationRecord_GetContextId(IntPtr record);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstUtf8StringMarshaller))]
    public static extern string TranslationRecord_GetMeta(IntPtr record);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TranslationRecord_SetTarget(IntPtr record,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport("mwtextlib.dll", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ConstUtf8StringMarshaller))]
    public static extern string Translation_GetLastError();
}
