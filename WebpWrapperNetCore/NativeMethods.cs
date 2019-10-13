using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace WebpWrapper
{
    [SuppressUnmanagedCodeSecurity]
    class NativeMethods
    {
        private const string dllName = "libwebp.dll";
        private const int WEBP_DECODER_ABI_VERSION = 0x0208;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        static NativeMethods()
        {
            var dllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "runtimes", "win-" + (Environment.Is64BitProcess ? "x64" : "x86"), "native", dllName);
            if (!(File.Exists(dllPath) && LoadLibrary(dllPath) != IntPtr.Zero))
            {
                throw new DllNotFoundException(dllPath);
            }
        }

        [DllImport(dllName, EntryPoint = "WebPGetFeaturesInternal", CallingConvention = CallingConvention.Cdecl)]
        public static extern VP8StatusCode WebPGetFeaturesInternal
            ([In()] IntPtr rawWebP, UIntPtr data_size, ref WebPBitstreamFeatures features, int WEBP_DECODER_ABI_VERSION);

        public static VP8StatusCode WebPGetFeatures(IntPtr rawWebP, int data_size, ref WebPBitstreamFeatures features)
            => WebPGetFeaturesInternal(rawWebP, (UIntPtr)data_size, ref features, WEBP_DECODER_ABI_VERSION);

        [DllImport(dllName, EntryPoint = "WebPGetInfo", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPGetInfo([In()] IntPtr data, UIntPtr data_size, out int width, out int height);

        [DllImport(dllName, EntryPoint = "WebPDecodeBGRInto", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPDecodeBGRInto
            ([In()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        [DllImport(dllName, EntryPoint = "WebPInitDecoderConfigInternal", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPInitDecoderConfigInternal
            (ref WebPDecoderConfig webPDecoderConfig, int WEBP_DECODER_ABI_VERSION);
        
        public static int WebPInitDecoderConfig(ref WebPDecoderConfig webPDecoderConfig)
            => WebPInitDecoderConfigInternal(ref webPDecoderConfig, WEBP_DECODER_ABI_VERSION);

        [DllImport(dllName, EntryPoint = "WebPDecode", CallingConvention = CallingConvention.Cdecl)]
        public static extern VP8StatusCode WebPDecode(IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);

        [DllImport(dllName, EntryPoint = "WebPFreeDecBuffer", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WebPFreeDecBuffer(ref WebPDecBuffer buffer);


    }
}
