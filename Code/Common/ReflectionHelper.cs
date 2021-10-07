using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Zynga.Core.Util
{
    public static partial class ReflectionHelper
    {
        public enum ASSEMBLIES
        {
            PRIMARY
        }

        public static Dictionary<ASSEMBLIES, string> ASSEMBLY_NAME_MAP = new Dictionary<ASSEMBLIES, string>
        {
            {ASSEMBLIES.PRIMARY, "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"}
        };

        private static List<Assembly> gameSpecificAssemblies = null;

        public static List<Assembly> GameCodeAssemblies
        {
            get
            {
                if (gameSpecificAssemblies != null)
                {
                    return gameSpecificAssemblies;
                }

                gameSpecificAssemblies = new List<Assembly>();
                foreach (Assembly asm in ReflectionHelper.Assemblies)
                {
                    //don't iterate through types in non game code
                    if (asm.FullName.FastStartsWith("Mono.Cecil") ||
                        asm.FullName.FastStartsWith("UnityScript") ||
                        asm.FullName.FastStartsWith("Boo.Lan") ||
                        asm.FullName.FastStartsWith("System") ||
                        asm.FullName.FastStartsWith("I18N") ||
                        asm.FullName.FastStartsWith("UnityEngine") ||
                        asm.FullName.FastStartsWith("UnityEditor") ||
                        asm.FullName.FastStartsWith("mscorlib"))
                    {
                        continue;
                    }

                    gameSpecificAssemblies.Add(asm);
                }

                return gameSpecificAssemblies;
            }
        }
        
        /// <summary>
        /// Attempst to return Assembly from the assembly cache by name
        /// </summary>
        /// <param name="name">name of the assembly</param>
        /// <param name="usePartial">will run with string.Contains() to avoid exact matches</param>
        /// <returns></returns>
        public static Assembly GetAssemblyByName(string name, bool usePartial = false)
        {
            if (Assemblies != null && Assemblies.Length > 0)
            {
                for (int i = 0; i < Assemblies.Length; ++i)
                {
                    if (usePartial && Assemblies[i].FullName.Contains(name))
                    {
                        return Assemblies[i];
                    }

                    if (Assemblies[i].FullName == name)
                    {
                        return Assemblies[i];
                    }
                }
            }

            return null;
        }
    }
}
