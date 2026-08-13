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

        public static bool IsDefined(this Enum value)
        {
            Type enumType = value.GetType();
            return Enum.IsDefined(enumType, value);
        }

        public static T[] ToArray<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        public static object[] ToNamedArray<T>() where T : Enum
        {
            return ToArray<T>().Select(e => new { Name = e.GetDescription(), Value = e }).ToArray();
        }

        public static bool GetBit<T>(this T val, int bit) where T : Enum
        {
            uint raw = Convert.ToUInt32(val);
            return (raw & (1 << bit)) != 0;
        }

        public static T SetBit<T>(this T val, int bit, bool set) where T : Enum
        {
            uint raw = Convert.ToUInt32(val);
            if (set)
            {
                raw |= 1u << bit;
            }
            else
            {
                raw &= ~(1u << bit);
            }
            return (T)Enum.ToObject(typeof(T), raw);
        }
    }
}
