using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SonicWave.Audio.Vst3;

[GeneratedComInterface]
[Guid("65ED9690-7164-4E13-8D2A-D5163FBECD9F")]
internal partial interface IPlugViewContentScaleSupport
{
    [PreserveSig]
    int SetContentScaleFactor(float factor);
}
