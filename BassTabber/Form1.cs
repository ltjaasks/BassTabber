using NAudio.Wave;
using System.Text;
using System.Timers;

namespace BassTabber
{
    public partial class Form1 : Form
    {
        NAudio.Wave.WaveInEvent? AaniIn;

        readonly double[] AudioAanitys;
        readonly double[] FftAanitys;

        readonly int SampleRate = 44100;
        readonly int BitDepth = 16;
        readonly int ChannelCount = 1;
        readonly int BufferMilliseconds = 50;
        int Tempo;
        int Isku;

        int PiikkiTaajuus;

        StringBuilder[] TabSb = new StringBuilder[4];
        List<String> Tahdit = new List<string>();

        int[][] oteLauta = new int[][]
        {
            new int[] {98, 104, 110, 117, 123, 131, 139, 147, 156, 165, 175, 185, 196, 208, 220, 233, 247, 262, 277, 294, 311},
            new int[] {73, 78, 82, 87, 92, 98, 104, 110, 117, 123, 131, 139, 147, 156, 165, 175, 185, 196, 208, 220, 233, 247},
            new int[] {55, 58, 62, 65, 69, 73, 78, 82, 87, 92, 98, 104, 110, 117, 123, 131, 139, 147, 156, 165, 175, 185, 196},
            new int[] {41, 44, 46, 49, 52, 55, 58, 62, 65, 69, 73, 78, 82, 87, 92, 98, 104, 110, 117, 123, 131, 139, 147, 156}
        };
         

        public Form1()
        {
            InitializeComponent();
            LisaaAaniLaitteet();

            for (int i = 0; i < TabSb.Length; i++)
                TabSb[i] = new StringBuilder("");

            AudioAanitys = new double[SampleRate * BufferMilliseconds / 1000];
            double[] PaddedAudio = FftSharp.Pad.ZeroPad(AudioAanitys);
            double[] FftMag = FftSharp.Transform.FFTmagnitude(PaddedAudio);
            FftAanitys = new double[FftMag.Length];

            double FftPeriod = FftSharp.Transform.FFTfreqPeriod(SampleRate, FftMag.Length);

            ///Alustetaan ‰‰nilaite input
            AaniIn = new NAudio.Wave.WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new NAudio.Wave.WaveFormat(rate: 44100, bits: 16, channels: 1),
                BufferMilliseconds = BufferMilliseconds
            };

            ///Kutsutaan tapahtumank‰sittelij‰‰ AaniIn_DataAvailable kun uutta dataa ‰‰nilaitteesta
            AaniIn.DataAvailable += AaniIn_DataAvailable;
        }


        private int GetTempo()
        {
            return Tempo;
        }


        /*
         * Lis‰‰ ‰‰nilaitteet listboxiin TO:DO k‰ytt‰j‰n valittavaksi
         */
        private void LisaaAaniLaitteet()
        {
            listBox1.BeginUpdate();

            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                listBox1.Items.Add(NAudio.Wave.WaveIn.GetCapabilities(i).ProductName);
            }

            listBox1.EndUpdate();
        }


        /*
         * Muuttaa puskurin bitit audio leveleiksi ja asettaa ne AudioAanitys taulukkoon
         */
        private void AaniIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            for (int i = 0; i < e.Buffer.Length / 2; i++)
            {
                AudioAanitys[i] = BitConverter.ToInt16(e.Buffer, i * 2);
            }

            if (AudioAanitys.Max() > 500)
                PiikkiTaajuus = AaniTaajuus();
            else PiikkiTaajuus = 0;
        }


        private int AaniTaajuus()
        {
            var window = new FftSharp.Windows.Hamming();
            double[] PaddedAudio = FftSharp.Pad.ZeroPad(AudioAanitys);
            double[] FftWindowed = window.Apply(PaddedAudio);
            double[] FftLowPass = FftSharp.Filter.LowPass(FftWindowed, SampleRate, maxFrequency: 300);
            
            double[] FftMag = FftSharp.Transform.FFTpower(FftLowPass);
            
            Array.Copy(FftMag, FftAanitys, FftMag.Length);
            //FftMag sis‰lt‰‰ fft datan viimeisest‰ audiosamplest‰?

            int Piikki = 0;
            for (int i = 0; i < FftMag.Length; i++)
            {
                if (FftMag[i] > FftMag[Piikki])
                    Piikki = i;
            }

            double FftPeriod = FftSharp.Transform.FFTfreqPeriod(SampleRate, FftMag.Length);
            double PiikkiTaajuus = FftPeriod * Piikki;

            return Convert.ToInt32(PiikkiTaajuus);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            AaniIn.StartRecording();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            AaniIn.StopRecording();
            AaniIn.Dispose();
            timer1.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = $"{PiikkiTaajuus:N0}";
            Kirjoita();
        }

        /// <summary>
        /// Etsii taulukosta oteLauta ‰‰nilaitteen taajuutta vastaavan v‰lin
        /// </summary>
        /// <param name="taajuus">ƒ‰nilaitteen poimima taajuus</param>
        /// <returns>
        /// Taajuutta vastaava v‰li tai -1
        /// </returns>
        private (int, int) EtsiVali(int taajuus)
        {
            for (int i = 0; i < oteLauta.Length; i++)
            {
                for (int j = oteLauta[i].Length - 1; j >= 0; j--)
                {
                    if (taajuus == oteLauta[i][j]) return (i, j);
                    if (taajuus > oteLauta[i][j])
                    {
                        return (i, j);
                    }
                }
            }
            return (-1, -1);
        }


        private void Kirjoita()
        {
            var Aani = (-1, -1);
            Isku++;
            if (PiikkiTaajuus != 0)
                Aani = EtsiVali(PiikkiTaajuus);

            
            for (int i = 0; i < TabSb.Length; i++)
            {
                if (i == Aani.Item1)
                {
                    TabSb[Aani.Item1].Append(Aani.Item2 + "ó");
                } else
                {
                    TabSb[i].Append("óó");
                }

                if (Isku == 32)
                    TabSb[i].Append("|");
            }

            if (Isku == 32)
            {
                StringBuilder Sb = new StringBuilder("");
                for (int i = 0; i < TabSb.Length; i++)
                {
                    Sb.Append(TabSb[i].ToString() + "\n");
                    TabSb[i].Clear();
                }
                Tahdit.Add(Sb.ToString());
                Isku = 0;
            }

            Tab.Clear();

            for (int i = 0; i < Tahdit.Count; i++)
                Tab.AppendText(Tahdit[i] + "\n");
            for (int i = 0; i < TabSb.Length; i++)
                Tab.AppendText(TabSb[i].ToString() + "\n");
        }


        private void tempoBox_TextChanged(object sender, EventArgs e)
        {
            int.TryParse(tempoBox.Text, out Tempo);
            label2.Text = $"{Tempo}bpm";
            if (Tempo != 0)
            {
                timer1.Interval = 15000 / Tempo;
            }
        }
    }
}