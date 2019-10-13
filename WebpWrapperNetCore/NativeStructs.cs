using System;
using System.Runtime.InteropServices;

namespace WebpWrapper
{

    public enum WEBP_CSP_MODE
    {
        MODE_RGB = 0, MODE_RGBA = 1,
        MODE_BGR = 2, MODE_BGRA = 3,
        MODE_ARGB = 4, MODE_RGBA_4444 = 5,
        MODE_RGB_565 = 6,
        // RGB-premultiplied transparent modes (alpha value is preserved)
        MODE_rgbA = 7,
        MODE_bgrA = 8,
        MODE_Argb = 9,
        MODE_rgbA_4444 = 10,
        // YUV modes must come after RGB ones.
        MODE_YUV = 11, MODE_YUVA = 12,  // yuv 4:2:0
        MODE_LAST = 13
    }

    public enum VP8StatusCode
    {
        VP8_STATUS_OK = 0,
        VP8_STATUS_OUT_OF_MEMORY,
        VP8_STATUS_INVALID_PARAM,
        VP8_STATUS_BITSTREAM_ERROR,
        VP8_STATUS_UNSUPPORTED_FEATURE,
        VP8_STATUS_SUSPENDED,
        VP8_STATUS_USER_ABORT,
        VP8_STATUS_NOT_ENOUGH_DATA
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPRGBABuffer
    {
        public IntPtr rgba;    // pointer to RGBA samples
        public int stride;       // stride in bytes from one scanline to the next.
        public UIntPtr size;      // total size of the *rgba buffer.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPYUVABuffer
    {
        public IntPtr y;     // pointer to luma, chroma U/V, alpha samples
        public IntPtr u;
        public IntPtr v;
        public IntPtr a;

        public int y_stride;               // luma stride
        public int u_stride;
        public int v_stride;     // chroma strides
        public int a_stride;               // alpha stride
        public UIntPtr y_size;              // luma plane size
        public UIntPtr u_size;
        public UIntPtr v_size;      // chroma planes size
        public UIntPtr a_size;              // alpha-plane size
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct WebPUnionBuffer
    {
        [FieldOffset(0)]
        public WebPRGBABuffer RGBA;

        [FieldOffset(0)]
        public WebPYUVABuffer YUVA;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPDecBuffer
    {
        public WEBP_CSP_MODE colorspace;  // Colorspace.
        public int width;
        public int height;         // Dimensions.

        // If non-zero, 'internal_memory' pointer is not
        // used. If value is '2' or more, the external
        // memory is considered 'slow' and multiple
        // read/write will be avoided.
        public int is_external_memory;

        public WebPUnionBuffer u; // Nameless union of buffer parameters.

        UInt32 pad0; // padding for later use
        UInt32 pad1;
        UInt32 pad2;
        UInt32 pad3;

        // Internally allocated memory (only when
        // is_external_memory is 0). Should not be used
        // externally, but accessed via the buffer union.
        private IntPtr private_memory;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPBitstreamFeatures
    {
        public int width;          // Width in pixels, as read from the bitstream.
        public int height;         // Height in pixels, as read from the bitstream.
        public int has_alpha;      // True if the bitstream contains an alpha channel.
        public int has_animation;  // True if the bitstream is an animation.
        public int format;         // 0 = undefined (/mixed), 1 = lossy, 2 = lossless

        UInt32 pad0; // padding for later use
        UInt32 pad1;
        UInt32 pad2;
        UInt32 pad3;
        UInt32 pad4;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPDecoderOptions
    {
        public int bypass_filtering;               // if true, skip the in-loop filtering
        public int no_fancy_upsampling;            // if true, use faster pointwise upsampler
        public int use_cropping;                   // if true, cropping is applied _first_
        public int crop_left;
        public int crop_top;            // top-left position for cropping. Will be snapped to even values.
        public int crop_width;
        public int crop_height;        // dimension of the cropping area
        public int use_scaling;                    // if true, scaling is applied _afterward_
        public int scaled_width;
        public int scaled_height;    // final resolution
        public int use_threads;                    // if true, use multi-threaded decoding
        public int dithering_strength;             // dithering strength (0=Off, 100=full)
        public int flip;                           // flip output vertically
        public int alpha_dithering_strength;       // alpha dithering strength in [0..100]

        UInt32 pad0; // padding for later use
        UInt32 pad1;
        UInt32 pad2;
        UInt32 pad3;
        UInt32 pad4;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPDecoderConfig
    {
        public WebPBitstreamFeatures input;  // Immutable bitstream features (optional)
        public WebPDecBuffer output;         // Output buffer (can point to external mem)
        public WebPDecoderOptions options;   // Decoding options
    }
}
