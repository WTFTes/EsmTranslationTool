using System;
using System.Runtime.InteropServices;

namespace TranslationLibrary;

class ConstUtf8StringMarshaller : ICustomMarshaler
{
    public object MarshalNativeToManaged(IntPtr pNativeData)
    {
        return Marshal.PtrToStringUTF8(pNativeData);
    }

    public IntPtr MarshalManagedToNative(object managedObj)
    {
        return IntPtr.Zero;
    }

    public void CleanUpNativeData(IntPtr pNativeData)
    {
    }

    public void CleanUpManagedData(object managedObj)
    {
    }

    public int GetNativeDataSize()
    {
        return -1;
    }

    static readonly ConstUtf8StringMarshaller Instance = new ConstUtf8StringMarshaller();

    public static ICustomMarshaler GetInstance(string cookie)
    {
        return Instance;
    }
}
