using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Xunit;

namespace WinterSled.Tests
{
    /// <summary>
    /// Catches a game update that breaks the plugin's Harmony patches. The compiler checks only that a patched method's
    /// name exists (nameof, against the publicized game); Harmony resolves the target and binds the patch's parameters
    /// by name only when the game loads, and a failure there stops the plugin's patching. Here every patch class is
    /// resolved the way Harmony does it, against the game assemblies the tests build with (CI: the latest dedicated
    /// server, fetched weekly), and each patch method's injected parameters are checked against its target.
    /// </summary>
    public class HarmonyPatchTests
    {
        private static readonly string[] PatchMethodNames = { "Prefix", "Postfix", "Transpiler", "Finalizer" };

        // Harmony's special parameters that don't name a parameter of the original
        private static readonly HashSet<string> Special = new HashSet<string>
        {
            "__instance", "__state", "__result", "__resultRef", "__originalMethod", "__args", "__runOriginal", "__exception",
        };

        // what Harmony's AccessTools.DeclaredMethod looks in (AccessTools itself doesn't load on .NET 10: its type
        // initializer probes the Mono runtime)
        private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private const BindingFlags AllFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static IEnumerable<Type> PatchClasses() => typeof(Plugin).Assembly.GetTypes()
            .Where(t => t.GetCustomAttributes<HarmonyPatch>(false).Any());

        public static IEnumerable<object[]> PatchClassNames() => PatchClasses().Select(t => new object[] { t.FullName });

        [Fact]
        public void ThePluginHasPatches()
        {
            // the list below would be empty (and pass) if the attributes stopped being found
            Assert.True(PatchClasses().Count() >= 8, "patch classes found: " + string.Join(", ", PatchClasses().Select(t => t.Name)));
        }

        [Theory]
        [MemberData(nameof(PatchClassNames))]
        public void PatchTargetsTheGame(string className)
        {
            var type = typeof(Plugin).Assembly.GetType(className);
            var original = Original(type);
            var errors = new List<string>();

            var prepare = type.GetMethod("Prepare", AllDeclared);
            if (prepare != null && prepare.GetParameters().Length == 0 && prepare.ReturnType == typeof(bool) && !(bool)prepare.Invoke(null, null))
            {
                errors.Add("Prepare() returns false: the patch would be skipped");
            }

            var patches = type.GetMethods(AllDeclared).Where(m => m.IsStatic && PatchMethodNames.Contains(m.Name)).ToList();
            if (patches.Count == 0)
            {
                errors.Add("no Prefix/Postfix/Transpiler/Finalizer method");
            }
            foreach (var patch in patches)
            {
                errors.AddRange(CheckParameters(patch, original).Select(e => $"{patch.Name}: {e}"));
                if (patch.Name == "Prefix" && patch.ReturnType != typeof(void) && patch.ReturnType != typeof(bool))
                {
                    errors.Add($"Prefix returns {patch.ReturnType.Name} (void or bool)");
                }
            }
            Assert.True(errors.Count == 0, $"{type.Name} → {original.DeclaringType.Name}.{original.Name}: {string.Join("; ", errors)}");
        }

        /// <summary>The patch's target as Harmony finds it: the class attributes merged, a declared method.</summary>
        private static MethodBase Original(Type type)
        {
            var target = type.GetMethod("TargetMethod", AllDeclared);
            if (target != null)
            {
                return (MethodBase)target.Invoke(null, null) ?? throw new Exception($"{type.Name}.TargetMethod() returns null");
            }
            // the class's attributes merged, later ones filling in what earlier ones left out
            Type declaringType = null;
            string methodName = null;
            Type[] argumentTypes = null;
            MethodType? methodType = null;
            foreach (var info in type.GetCustomAttributes<HarmonyPatch>(false).Select(a => a.info))
            {
                declaringType = info.declaringType ?? declaringType;
                methodName = info.methodName ?? methodName;
                argumentTypes = info.argumentTypes ?? argumentTypes;
                methodType = info.methodType ?? methodType;
            }
            Assert.True(declaringType != null && methodName != null, $"{type.Name}: no type or method name in its [HarmonyPatch]");
            Assert.True(methodType == null || methodType == MethodType.Normal, $"{type.Name}: method type {methodType} isn't checked here: extend the test");
            MethodInfo method;
            try
            {
                method = argumentTypes == null
                    ? declaringType.GetMethod(methodName, AllDeclared)
                    : declaringType.GetMethod(methodName, AllDeclared, null, argumentTypes, null);
            }
            catch (AmbiguousMatchException)
            {
                throw new Exception($"{type.Name}: {declaringType.Name}.{methodName} has overloads now: name the argument types in [HarmonyPatch]");
            }
            return method ?? throw new Exception($"{type.Name}: {declaringType.Name}.{methodName} is gone (game update?)");
        }

        private static IEnumerable<string> CheckParameters(MethodInfo patch, MethodBase original)
        {
            var originalParams = original.GetParameters();
            var returnType = (original as MethodInfo)?.ReturnType ?? typeof(void);
            foreach (var p in patch.GetParameters())
            {
                var type = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                if (p.Name == "__instance")
                {
                    if (original.IsStatic)
                    {
                        yield return "__instance on a static method";
                    }
                    else if (!type.IsAssignableFrom(original.DeclaringType))
                    {
                        yield return $"__instance is {type.Name}, the method is on {original.DeclaringType.Name}";
                    }
                }
                else if (p.Name == "__result" && returnType == typeof(void))
                {
                    yield return "__result on a void method";
                }
                else if (p.Name.StartsWith("___"))
                {
                    if (FieldUp(original.DeclaringType, p.Name.Substring(3)) == null)
                    {
                        yield return $"field {p.Name.Substring(3)} is gone from {original.DeclaringType.Name}";
                    }
                }
                else if (Special.Contains(p.Name))
                {
                }
                else if (p.Name.StartsWith("__") && int.TryParse(p.Name.Substring(2), out int index))
                {
                    if (index >= originalParams.Length)
                    {
                        yield return $"{p.Name}: the method has {originalParams.Length} parameters";
                    }
                }
                else if (patch.Name != "Transpiler")
                {
                    var match = originalParams.FirstOrDefault(o => o.Name == p.Name);
                    if (match == null)
                    {
                        yield return $"parameter {p.Name} is gone (the method has: {string.Join(", ", originalParams.Select(o => o.Name))})";
                    }
                    else
                    {
                        var originalType = match.ParameterType.IsByRef ? match.ParameterType.GetElementType() : match.ParameterType;
                        if (!type.IsAssignableFrom(originalType))
                        {
                            yield return $"parameter {p.Name} is {originalType.Name} now, the patch takes {type.Name}";
                        }
                    }
                }
            }
        }

        // Harmony's ___field injection looks up the class hierarchy
        private static FieldInfo FieldUp(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var f = t.GetField(name, AllFields | BindingFlags.DeclaredOnly);
                if (f != null)
                {
                    return f;
                }
            }
            return null;
        }

        [Fact]
        public void RemoteSeatPoseUsesTheGamesZdoKeys()
        {
            // the rider's seat flag is read from the ZDO under ZSyncAnimation's own key offset
            var salt = typeof(ZSyncAnimation).GetField("c_ZDOSalt", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(salt);
            Assert.Equal(RemoteSeatPose.ZdoSalt, (int)salt.GetRawConstantValue());
        }
    }
}
