using System;
using System.IO;
using System.Media;
using System.Text;

namespace AscentBellTool
{
    /// Built-in sounds are synthesised in memory, so there are no audio files to ship or lose.
    /// Windows' PlaySound plays one sound per process: while the ring loop runs, chimes are skipped.
    public static class Sound
    {
        private static SoundPlayer loopPlayer;
        private static int loopUsers;
        private static SoundPlayer chimePlayer;

        private static byte[] ringWav;
        private static byte[] chimeWav;

        public static void StartLoop(string customFile)
        {
            loopUsers++;
            if (loopPlayer != null) return;
            loopPlayer = CreatePlayer(customFile, ring: true);
            try { loopPlayer.PlayLooping(); }
            catch (Exception ex)
            {
                // Custom file broken/missing → fall back to the built-in bell.
                BellLog.Write("Sound file failed (" + ex.Message + ") — using built-in bell.");
                loopPlayer = CreatePlayer(null, ring: true);
                loopPlayer.PlayLooping();
            }
        }

        public static void StopLoop()
        {
            if (--loopUsers > 0) return;
            loopUsers = 0;
            if (loopPlayer == null) return;
            loopPlayer.Stop();
            loopPlayer.Dispose();
            loopPlayer = null;
        }

        public static void PlayChime()
        {
            if (loopPlayer != null) return;
            try
            {
                chimePlayer?.Dispose();
                chimePlayer = new SoundPlayer(new MemoryStream(ChimeWav));
                chimePlayer.Play();
            }
            catch { /* a missed chime is acceptable; the ring alert is the important one */ }
        }

        /// Plays the ring sound for a few seconds (Settings → Test sound).
        public static void PlayRingOnce(string customFile)
        {
            if (loopPlayer != null) return;
            try
            {
                chimePlayer?.Dispose();
                chimePlayer = CreatePlayer(customFile, ring: true);
                chimePlayer.Play();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Could not play the sound: " + ex.Message, Program.AppTitle);
            }
        }

        private static SoundPlayer CreatePlayer(string customFile, bool ring)
        {
            if (!string.IsNullOrWhiteSpace(customFile) && File.Exists(customFile))
                return new SoundPlayer(customFile);
            return new SoundPlayer(new MemoryStream(ring ? RingWav : ChimeWav));
        }

        private const int Rate = 22050;

        private static byte[] RingWav => ringWav ?? (ringWav = MakeRing());
        private static byte[] ChimeWav => chimeWav ?? (chimeWav = MakeChime());

        /// Electric-school-bell style: a hammer striking a gong ~18 times a second for 1.4 s,
        /// then 0.6 s to decay — 2 s per loop.
        private static byte[] MakeRing()
        {
            const double total = 2.0, trill = 1.4, strikeRate = 18, tau = 0.05;
            int n = (int)(Rate * total);
            var buf = new double[n];
            int strikes = (int)(trill * strikeRate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double env = 0;
                int last = Math.Min(strikes, (int)(t * strikeRate));
                for (int k = 0; k <= last; k++)
                {
                    double dt = t - k / strikeRate;
                    if (dt >= 0) env += Math.Exp(-dt / tau);
                }
                double tone = Math.Sin(2 * Math.PI * 940 * t)
                            + 0.5 * Math.Sin(2 * Math.PI * 2350 * t)
                            + 0.25 * Math.Sin(2 * Math.PI * 3720 * t);
                buf[i] = env * tone;
            }
            return ToWav(buf);
        }

        /// Two-note "ding-dong" heads-up.
        private static byte[] MakeChime()
        {
            const double total = 1.6;
            int n = (int)(Rate * total);
            var buf = new double[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                buf[i] = Note(t, 0.0, 1047) + Note(t, 0.55, 784);
            }
            return ToWav(buf);
        }

        private static double Note(double t, double start, double f)
        {
            double dt = t - start;
            if (dt < 0) return 0;
            return Math.Exp(-dt / 0.35) * (Math.Sin(2 * Math.PI * f * dt) + 0.25 * Math.Sin(2 * Math.PI * 2 * f * dt));
        }

        private static byte[] ToWav(double[] samples)
        {
            double peak = 1e-9;
            foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
            double scale = 0.85 * short.MaxValue / peak;

            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int dataLen = samples.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataLen);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);          // PCM
                w.Write((short)1);          // mono
                w.Write(Rate);
                w.Write(Rate * 2);          // byte rate
                w.Write((short)2);          // block align
                w.Write((short)16);         // bits per sample
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataLen);
                foreach (var s in samples) w.Write((short)Math.Round(s * scale));
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
