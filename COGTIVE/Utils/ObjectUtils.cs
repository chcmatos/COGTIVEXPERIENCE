using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace COGTIVE.Utils
{
    internal class ObjectUtils
    {
        public static T RequireNonNull<T>(T t, [CallerMemberName] string name = null) where T : class
        {
            return t ?? throw new ArgumentNullException(name);
        }

        public static string RequireNonNullOrEmpty(string str, [CallerMemberName] string name = null)
        {
            return RequireNonNull(str, name).Any() ? str : throw new ArgumentException($"\"{name}\" can not be empty!");
        }

    }
}
