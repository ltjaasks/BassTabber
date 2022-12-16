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

        double PiikkiTaajuus;

        StringBuilder[] TabSb = new StringBuilder[4];



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


        private double AaniTaajuus()
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

            return PiikkiTaajuus;
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
            for (int i = 0; i < TabSb.Length; i++)
                TabSb[i].Append("ó");
            
            Tab.Clear();
            for (int i = 0; i < TabSb.Length; i++)
                Tab.AppendText(TabSb[i].ToString() + "\n");
        }


    }
}