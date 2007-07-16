// Thanks to Jonathan Pryor and Jonathan Chambers from the mono general
// mailing list for their assistance with custom marshalers

#if (UCS4)
using System;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix;

namespace Python.Runtime {
        public class Utf32Marshaler : ICustomMarshaler {
                private static Utf32Marshaler instance = new
                Utf32Marshaler ();
                
                public static ICustomMarshaler GetInstance (string s)
                {
                        return instance;
                }
                
                public void CleanUpManagedData (object o)
                {
                }
                
                public void CleanUpNativeData (IntPtr pNativeData)
                {
                        UnixMarshal.FreeHeap (pNativeData);
                }
                
                public int GetNativeDataSize ()
                {
                        return IntPtr.Size;
                }
                
                public IntPtr MarshalManagedToNative (object obj)
                {
                        string s = obj as string;
                        if (s == null)
                                return IntPtr.Zero;
                        return UnixMarshal.StringToHeap (s,
                        Encoding.UTF32);
                }
                
                public object MarshalNativeToManaged (IntPtr
                pNativeData)
                {
                        return UnixMarshal.PtrToString (pNativeData, 
                                Encoding.UTF32);
                }
        }
}
#endif

