using System;
using System.ComponentModel;

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

    public class DisplayNotFoundException : Exception
    {
        public DisplayNotFoundException() 
            : base($"Display not found") 
        { }
    }

    public class ColorProfileOperationException : Exception
    {
        public ColorProfileOperationException(string operation, string profilePath, int errorCode) 
            : base($"{operation} failed for '{profilePath}' with error {errorCode}: {new Win32Exception(errorCode).Message}")
        { }
    }

    public class ExternalAPIException : Exception
    {
        public ExternalAPIException(string message) : base(message) { }

        public ExternalAPIException(string function, int errorCode)
            : base($"{function} failed with error code {errorCode}")
        { }
    }
}