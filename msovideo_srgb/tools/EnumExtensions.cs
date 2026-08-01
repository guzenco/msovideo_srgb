using System.ComponentModel;
using System;
using System.Linq;

namespace msovideo_srgb
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attr?.Description ?? value.ToString();
        }

        public static T[] ToArray<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        public static object[] ToNamedArray<T>() where T : Enum
        {
            return ToArray<T>().Select(e => new { Name = e.GetDescription(), Value = e }).ToArray();
        }
    }
}
