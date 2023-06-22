using System.Collections.Generic;

namespace Hathora.Core.Scripts.Runtime.Common.Extensions
{
    public static class ListExtensions
    {
        public static void AddIfNotNull<T>(this List<T> list, T item)
        {
            if (item != null)
                list.Add(item);
        }
    }
}
