using System;

namespace msovideo_srgb
{
    public class ICCProfileException : FormatException
    {
        public ICCProfileException(string message) : base(message) { }
    }

    public class DisplayNotFoundException : InvalidOperationException
    {
        public DisplayNotFoundException(string message) : base(message) { }
    }
}