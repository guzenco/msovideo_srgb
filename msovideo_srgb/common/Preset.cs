namespace msovideo_srgb
{
    public class Preset
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Hotkey Hotkey { get; set; }

        public Preset(int id, string name, Hotkey hotkey = null)
        {
            Id = id;
            Name = name;
            Hotkey = hotkey?? new Hotkey();
        }
    }
}
