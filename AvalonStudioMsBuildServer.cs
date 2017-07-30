using AsyncRpc;
using AsyncRpc.Routing;
using AsyncRpc.Transport.Tcp;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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

        public Task<MsBuildHostServiceResponse<TaskItems>> GetTaskItem(string target, string projectFile, List<Property> properties)
        {
            var outputs = new Dictionary<string, ITaskItem[]>();

            var props = new Dictionary<string, string>();
            
            foreach(var prop in properties)
            {
                props.Add(prop.Key, prop.Value);
            }

            bool fullFramework = true;

            using (var textReader = XmlReader.Create(projectFile))
            {
                if(textReader.ReadToFollowing("Project"))
                {
                    fullFramework = textReader.GetAttribute("Sdk") == null;
                }
            }

            var buildTargets = new[] { "Compile" };
            _buildEngine.BuildProjectFile(projectFile, buildTargets, props, outputs);                
                       

            var result = new TaskItems { Target = target };

            if (outputs.ContainsKey(target))
            {
                foreach (var item in outputs[target])
                {
                    var taskItem = new TaskItem
                    {
                        ItemSpec = item.ItemSpec
                    };

                    foreach (string metaData in item.MetadataNames)
                    {
                        var metaDataObj = new ProjectTaskMetaData { Name = metaData.Replace("\0", ""), Value = item.GetMetadata(metaData).Replace("\0", "") };

                        taskItem.Metadatas.Add(metaDataObj);
                    }

                    result.Items.Add(taskItem);
                }
            }

            return Task.FromResult(new MsBuildHostServiceResponse<TaskItems> { Response = "OK", Data = result });
        }

        public Task<MsBuildHostServiceResponse<List<string>>> GetAssemblyReferences(string projectFile)
        {
            var outputs = new Dictionary<string, ITaskItem[]>();
            var properties = new Dictionary<string, string>
            {
                    { "TargetFramework", "netcoreapp2.0" }
            };

            // GenerateAssemblyInfo,_CheckForInvalidConfigurationAndPlatform,BuildOnlySettings,GetFrameworkPaths,BeforeResolveReferences,ResolveAssemblyReferences,ResolveComReferences,ImplicitlyExpandDesignTimeFacades,ResolveSDKReferences
            _buildEngine.BuildProjectFile(projectFile, new[] { "GenerateAssemblyInfo", "_CheckForInvalidConfigurationAndPlatform", "BuildOnlySettings", "GetFrameworkPaths", "BeforeResolveReferences", "ResolveAssemblyReferences", "ResolveComReferences", "ResolveSDKReferences", "GetOutputs" }, properties, outputs);

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

        public Task<MsBuildHostServiceResponse<List<string>>> GetProjectReferences(string projectFile)
        {
            var document = XDocument.Load(projectFile);

            var references = document.Descendants("ProjectReference").Select(e => e.Attribute("Include").Value).ToList();

            return Task.FromResult(new MsBuildHostServiceResponse<List<string>> { Response = "OK", Data = references });
        }

        public Task<MsBuildHostServiceResponse<List<string>>> GetProjectFrameworks(string projectFile)
        {
            var document = XDocument.Load(projectFile);

            var targetFramework = document.Descendants("TargetFramework").FirstOrDefault();
            var targetFrameworks = document.Descendants("TargetFrameworks");

            var result = new List<string>();

            if(targetFramework != null)
            {
                result.Add(targetFramework.Value);
            }

            if(targetFrameworks != null)
            {
                foreach(var framework in targetFrameworks)
                {
                    result.Add(framework.Value);
                }
            }

            return Task.FromResult(new MsBuildHostServiceResponse<List<string>> { Response = "OK", Data = result });
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
