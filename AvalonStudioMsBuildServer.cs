using AsyncRpc;
using AsyncRpc.Routing;
using AsyncRpc.Transport.Tcp;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AvalonStudio.MSBuildHost
{
    public class MsBuildHostServiceResponse<T>
    {
        public string Response { get; set; }

        public T Data { get; set; }
    }

    public class ProjectTaskMetaData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class TaskItem
    {
        public TaskItem()
        {
            Metadatas = new List<ProjectTaskMetaData>();
        }

        public string ItemSpec { get; set; }

        public List<ProjectTaskMetaData> Metadatas { get; set; }
    }

    public class TaskItems
    {
        public TaskItems()
        {
            Items = new List<TaskItem>();
        }

        public string Target { get; set; }

        public List<TaskItem> Items { get; set; }
    }

    public class MSBuildHostService : IMsBuildHostService
    {
        private IBuildEngine _buildEngine;

        public MSBuildHostService(IBuildEngine buildEngine)
        {
            _buildEngine = buildEngine;
        }

        public Task<MsBuildHostServiceResponse<TaskItems>> GetTaskItem(string target, string projectFile)
        {
            var outputs = new Dictionary<string, ITaskItem[]>();
            var properties = new Dictionary<string, string>
            {
                    { "TargetFramework", "netcoreapp2.0" }
            };

            // GenerateAssemblyInfo,_CheckForInvalidConfigurationAndPlatform,BuildOnlySettings,GetFrameworkPaths,BeforeResolveReferences,ResolveAssemblyReferences,ResolveComReferences,ImplicitlyExpandDesignTimeFacades,ResolveSDKReferences
            _buildEngine.BuildProjectFile(projectFile, new[] { "GenerateAssemblyInfo", "_CheckForInvalidConfigurationAndPlatform", "BuildOnlySettings", "GetFrameworkPaths", "BeforeResolveReferences", target, "ResolveComReferences", "ResolveSDKReferences" }, properties, outputs);

            var result = new TaskItems {  Target = target };

             foreach (var item in outputs[target])
             {
                 var taskItem = new TaskItem
                 {
                     ItemSpec = item.ItemSpec
                 };

                 foreach (string metaData in item.MetadataNames)
                 {
                     taskItem.Metadatas.Add(new ProjectTaskMetaData { Name = metaData, Value = item.GetMetadata(metaData) });
                 }

                 result.Items.Add(taskItem);

                break;
             }

            return Task.FromResult(new MsBuildHostServiceResponse<TaskItems> { Response = "OK", Data = result });
        }

        public Task<MsBuildHostServiceResponse<List<string>>> GetReferences(string projectFile)
        {
            var outputs = new Dictionary<string, ITaskItem[]>();
            var properties = new Dictionary<string, string>
            {
                    { "TargetFramework", "netcoreapp2.0" }
            };

            // GenerateAssemblyInfo,_CheckForInvalidConfigurationAndPlatform,BuildOnlySettings,GetFrameworkPaths,BeforeResolveReferences,ResolveAssemblyReferences,ResolveComReferences,ImplicitlyExpandDesignTimeFacades,ResolveSDKReferences
            _buildEngine.BuildProjectFile(projectFile, new[] { "GenerateAssemblyInfo", "_CheckForInvalidConfigurationAndPlatform", "BuildOnlySettings", "GetFrameworkPaths", "BeforeResolveReferences", "ResolveAssemblyReferences", "ResolveComReferences", "ResolveSDKReferences" }, properties, outputs);

            foreach (var taskItem in outputs)
            {
                foreach (var item in taskItem.Value)
                {
                    Console.Write(item.ItemSpec);

                    foreach (string metaData in item.MetadataNames)
                    {
                        Console.WriteLine($"{metaData}:{item.GetMetadata(metaData)}");
                    }
                }
            }
            
            return Task.FromResult(new MsBuildHostServiceResponse<List<string>> { Response = "OK", Data = outputs["ResolveAssemblyReferences"].Select(ti => ti.ItemSpec).ToList() });
        }

        public Task<string> GetVersion()
        {
            return Task.FromResult("1.02");
        }
    }

    public class AvalonStudioTask : ITask
    {
        private static void StartSever(IBuildEngine buildEngine)
        {
            var router = new DefaultTargetSelector();
            router.Register<IMsBuildHostService, MSBuildHostService>(new MSBuildHostService(buildEngine));

            var host = new TcpHost(new Engine().CreateRequestHandler(router));
            host.StartListening(new System.Net.IPEndPoint(IPAddress.Loopback, 9000));
        }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            StartSever(BuildEngine);

            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            return true;
        }
    }
}
