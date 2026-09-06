namespace msovideo_srgb
{
    public class Dithering
    {
        public Dithering(int state, int bits, int mode)
        {
            State = state;
            Bits = bits;
            Mode = mode;
        }

        public Dithering(int state, int bits, int mode, string[] stateNames, string[] bitsNames, string[] modeNames)
        {
            State = state;
            Bits = bits;
            Mode = mode;
            StateNames = stateNames;
            BitsNames = bitsNames;
            ModeNames = modeNames;
        }

        public int State { get; set; }
        public int Bits { get; set; }
        public int Mode { get; set; }

        public string[] StateNames { get; }
        public string[] BitsNames { get; }
        public string[] ModeNames { get; }

        public bool StateAvailable => StateNames.Length > 0;
        public bool BitsAvailable => BitsNames.Length > 0;
        public bool ModeAvailable => ModeNames.Length > 0;

    }
}
