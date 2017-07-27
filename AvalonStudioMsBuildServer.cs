using AsyncRpc;
using AsyncRpc.Routing;
using AsyncRpc.Transport.Tcp;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AvalonStudio.MSBuildHost
{
    public class AvalonStudioTask : ITask
    {
        class Service : IMsBuildHostService
        {
            public Task<Version> GetVersion()
            {
                return Task.FromResult(Version.Parse("1.02"));
            }
        }

        private static void StartSever()
        {
            var router = new DefaultTargetSelector();
            router.Register<IMsBuildHostService, Service>();

            var host = new TcpHost(new Engine().CreateRequestHandler(router));
            host.StartListening(new System.Net.IPEndPoint(IPAddress.Loopback, 9000));
        }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            StartSever();

            while(true)
            {
                var outputs = new Dictionary<string, ITaskItem[]>();
                var properties = new Dictionary<string, string>
                {
                    { "TargetFramework", "netcoreapp2.0" }
                };

                Console.WriteLine("Hello from BuildTask");

                // GenerateAssemblyInfo,_CheckForInvalidConfigurationAndPlatform,BuildOnlySettings,GetFrameworkPaths,BeforeResolveReferences,ResolveAssemblyReferences,ResolveComReferences,ImplicitlyExpandDesignTimeFacades,ResolveSDKReferences
                BuildEngine.BuildProjectFile(@"c:\dev\repos\AvalonStudio\AvalonStudio\AvalonStudio\AvalonStudio.csproj", new[] { "GenerateAssemblyInfo", "_CheckForInvalidConfigurationAndPlatform", "BuildOnlySettings", "GetFrameworkPaths", "BeforeResolveReferences", "ResolveAssemblyReferences", "ResolveComReferences", "ResolveSDKReferences" }, properties, outputs);

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
