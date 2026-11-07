using System;
using System.Runtime.InteropServices;

namespace AscentBellTool
{
    /// Unmutes the default speakers and raises the master volume (Windows Core Audio, Vista+).
    /// A muted PC is the most common reason a bell alert goes unheard. Every failure is ignored.
    public static class AudioVolume
    {
        public static void EnsureAudible(int minPercent)
        {
            try
            {
                var vol = GetEndpointVolume();
                if (vol == null) return;
                var ctx = Guid.Empty;
                vol.SetMute(false, ref ctx);
                vol.GetMasterVolumeLevelScalar(out float current);
                float target = Math.Max(0, Math.Min(100, minPercent)) / 100f;
                if (current < target) vol.SetMasterVolumeLevelScalar(target, ref ctx);
                Marshal.ReleaseComObject(vol);
            }
            catch
            {
                // No audio device / driver quirk — the visual alert still works.
            }
        }

        private static IAudioEndpointVolume GetEndpointVolume()
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            try
            {
                enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out IMMDevice device);
                if (device == null) return null;
                var iid = typeof(IAudioEndpointVolume).GUID;
                device.Activate(ref iid, 23 /* CLSCTX_ALL */, IntPtr.Zero, out object o);
                Marshal.ReleaseComObject(device);
                return o as IAudioEndpointVolume;
            }
            finally
            {
                Marshal.ReleaseComObject(enumerator);
            }
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorCom { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            void EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
            void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            void Activate(ref Guid iid, int clsCtx, IntPtr activationParams,
                          [MarshalAs(UnmanagedType.IUnknown)] out object iface);
        }

        // Method order must match the native vtable exactly, so unused methods are declared too.
        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            void RegisterControlChangeNotify(IntPtr notify);
            void UnregisterControlChangeNotify(IntPtr notify);
            void GetChannelCount(out uint count);
            void SetMasterVolumeLevel(float levelDb, ref Guid ctx);
            void SetMasterVolumeLevelScalar(float level, ref Guid ctx);
            void GetMasterVolumeLevel(out float levelDb);
            void GetMasterVolumeLevelScalar(out float level);
            void SetChannelVolumeLevel(uint channel, float levelDb, ref Guid ctx);
            void SetChannelVolumeLevelScalar(uint channel, float level, ref Guid ctx);
            void GetChannelVolumeLevel(uint channel, out float levelDb);
            void GetChannelVolumeLevelScalar(uint channel, out float level);
            void SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid ctx);
            void GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
        }
    }
}
