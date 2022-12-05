using System;
using System.ComponentModel;
using System.Linq;

namespace WakaTime
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum e)
        {
            if (e is null) return null;

            var attr = (DescriptionAttribute)e.GetType().GetField(e.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
            return attr?.Description ?? e.ToString();
        }
    }
}
