using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Blazor.SPA.Utilities
{
    public static class Utils
    {
        static public IEnumerable<Type> GetTypesWithCustomAttribute(Assembly assembly, Type attribute)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(attribute, true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        static public IEnumerable<Type> GetTypeListWithCustomAttribute(Assembly assembly, Type attribute)
            => assembly.GetTypes().Where(item => (item.GetCustomAttributes(attribute, true).Length > 0));
    }
}
