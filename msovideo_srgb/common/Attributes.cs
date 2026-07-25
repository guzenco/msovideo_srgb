using System.Reflection;
using System;

namespace msovideo_srgb
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PersistentAttribute : Attribute
    {
        public string Key { get; }
        public object DefaultValue { get; }

        public PersistentAttribute(string key, object defaultValue = null)
        {
            Key = key;
            DefaultValue = defaultValue;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BindToPropertyAttribute : Attribute
    {
        public PropertyInfo Property { get; }

        public BindToPropertyAttribute(Type type, string propertyName)
        {
            Property = type.GetProperty(propertyName);
        }
    }
}
