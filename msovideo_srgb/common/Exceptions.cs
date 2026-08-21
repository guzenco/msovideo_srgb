using System;

namespace msovideo_srgb
{
    public class ICCProfileException : FormatException
    {
        public ICCProfileException(string message) : base(message) { }
    }

    public class EDIDException : FormatException
    {
        public EDIDException(string message) : base(message) { }
    }
}