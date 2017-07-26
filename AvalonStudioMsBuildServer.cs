using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;

namespace msbuild_interceptor
{
    public class AvalonStudioTask : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            while(true)
            {
                var outputs = new Dictionary<string, ITaskItem[]>();
                var properties = new Dictionary<string, string>
                {
                    { "TargetFramework", "netcoreapp2.0" }
                };

                Console.WriteLine("Hello from BuildTask");

                // GenerateAssemblyInfo,_CheckForInvalidConfigurationAndPlatform,BuildOnlySettings,GetFrameworkPaths,BeforeResolveReferences,ResolveAssemblyReferences,ResolveComReferences,ImplicitlyExpandDesignTimeFacades,ResolveSDKReferences
                BuildEngine.BuildProjectFile(@"C:\Users\dan\Documents\GitHub\Avalonia\samples\ControlCatalog.Desktop\ControlCatalog.Desktop.csproj", new[] { "GenerateAssemblyInfo", "_CheckForInvalidConfigurationAndPlatform", "BuildOnlySettings", "GetFrameworkPaths", "BeforeResolveReferences", "ResolveAssemblyReferences", "ResolveComReferences", "ResolveSDKReferences" }, properties, outputs);

                foreach(var output in outputs)
                {
                    Console.WriteLine($"{output.Key}: {output.Value.Length}");

                    foreach(var item in output.Value)
                    {
                        Console.WriteLine(item.ItemSpec);
                    }
                }

                if(Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            return true;
        }
    }
}
