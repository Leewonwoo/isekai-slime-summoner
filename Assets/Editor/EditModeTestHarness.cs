using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Editor
{
    /// <summary>
    /// Fallback runner for this prototype's legacy no-asmdef EditMode tests.
    /// Unity Test Runner does not discover tests compiled into Assembly-CSharp-Editor.
    /// </summary>
    public static class EditModeTestHarness
    {
        [MenuItem("Isekai Slime Summoner/Tests/Run Legacy EditMode Tests")]
        public static void Run()
        {
            Type[] testTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => type.Namespace == "CrossDefense.Tests.EditMode")
                .OrderBy(type => type.FullName)
                .ToArray();
            int passed = 0;
            var failures = new List<string>();

            for (int typeIndex = 0; typeIndex < testTypes.Length; typeIndex++)
            {
                Type type = testTypes[typeIndex];
                MethodInfo setUp = FindAttributedMethod<SetUpAttribute>(type);
                MethodInfo tearDown = FindAttributedMethod<TearDownAttribute>(type);
                MethodInfo[] methods = type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(method =>
                        method.GetCustomAttribute<TestAttribute>() != null ||
                        method.GetCustomAttributes<TestCaseAttribute>().Any())
                    .OrderBy(method => method.Name)
                    .ToArray();

                for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    MethodInfo method = methods[methodIndex];
                    TestCaseAttribute[] cases = method.GetCustomAttributes<TestCaseAttribute>().ToArray();
                    if (cases.Length == 0)
                        Execute(type, setUp, tearDown, method, Array.Empty<object>(), ref passed, failures);
                    else
                        for (int caseIndex = 0; caseIndex < cases.Length; caseIndex++)
                            Execute(
                                type,
                                setUp,
                                tearDown,
                                method,
                                cases[caseIndex].Arguments,
                                ref passed,
                                failures);
                }
            }

            if (failures.Count > 0)
                throw new AssertionException(
                    $"Legacy EditMode tests failed: {failures.Count}\n{string.Join("\n", failures)}");
            Debug.Log($"[CrossDefense] Legacy EditMode tests passed: {passed}.");
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void Execute(
            Type type,
            MethodInfo setUp,
            MethodInfo tearDown,
            MethodInfo test,
            object[] arguments,
            ref int passed,
            List<string> failures)
        {
            object instance = Activator.CreateInstance(type);
            string caseName = $"{type.Name}.{test.Name}" +
                              (arguments.Length > 0 ? $"({string.Join(", ", arguments)})" : string.Empty);
            try
            {
                setUp?.Invoke(instance, null);
                test.Invoke(instance, arguments);
                passed++;
            }
            catch (Exception exception)
            {
                Exception cause = exception is TargetInvocationException invocation &&
                                  invocation.InnerException != null
                    ? invocation.InnerException
                    : exception;
                failures.Add($"{caseName}: {cause.Message}");
            }
            finally
            {
                try
                {
                    tearDown?.Invoke(instance, null);
                }
                catch (Exception exception)
                {
                    failures.Add($"{caseName} teardown: {exception.Message}");
                }
            }
        }

        static MethodInfo FindAttributedMethod<T>(Type type) where T : Attribute =>
            type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method => method.GetCustomAttribute<T>() != null);

        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}
