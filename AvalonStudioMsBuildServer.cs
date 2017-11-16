using AsyncRpc;
using AsyncRpc.Routing;
using AsyncRpc.Transport.Tcp;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AvalonStudio.MSBuildHost
{
    public class ProjectTaskMetaData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class MetaDataReference
    {
        public string Assembly { get; set; }
        public List<ProjectTaskMetaData> MetaData { get; set; } = new List<ProjectTaskMetaData>();
    }

    public class MSBuildHostService : IMsBuildHostService
    {
        private IBuildEngine _buildEngine;
        private TaskCompletionSource<bool> _serverCompleted;

        public MSBuildHostService(IBuildEngine buildEngine)
        {
            _buildEngine = buildEngine;
            _serverCompleted = new TaskCompletionSource<bool>();
        }

        public Task<bool> ServerTask => _serverCompleted.Task;

        public async Task<MsBuildProjectInformation> LoadProject(string solutionDirectory, string projectFile, string targetFramework = null)
        {
            Console.WriteLine($"Loading Project: {Path.GetFileName(projectFile)}");
            var outputs = new Dictionary<string, ITaskItem[]>();

            var props = new Dictionary<string, string>
            {
                { "DesignTimeBuild", "true" },
                { "BuildProjectReferences",  "false" },
                { "_ResolveReferenceDependencies",  "true" },
                { "SolutionDir",  solutionDirectory },
                {  "ProvideCommandLineInvocation",  "true" },
                {  "SkipCompilerExecution",  "true" }
            };

            /*bool fullFramework = true;

            using (var textReader = XmlReader.Create(projectFile))
            {
                if (textReader.ReadToFollowing("Project"))
                {
                    fullFramework = textReader.GetAttribute("Sdk") == null;
                }
            }*/

            var document = XDocument.Load(projectFile);

            var projectReferences = document.Descendants("ProjectReference").Select(e => e.Attribute("Include").Value).ToList();

            if(targetFramework == null)
            {
                var frameworks = await GetTargetFrameworks(projectFile);

                targetFramework = frameworks.FirstOrDefault();

                if(targetFramework != null)
                {
                    Console.WriteLine($"Automatically selecting {targetFramework} as TargetFramework");
                    props.Add("TargetFramework", targetFramework);
                }
            }
            else
            {
                Console.WriteLine($"Manually selecting {targetFramework} as TargetFramework");
            }
            if(_buildEngine.BuildProjectFile(projectFile, new[] { "ResolveAssemblyReferences", "GetTargetPath", "Compile" }, props, outputs))
            {
                Console.WriteLine("Project loaded successfully");

                var metaDataReferences = new List<MetaDataReference>();

                if (outputs.ContainsKey("ResolveAssemblyReferences"))
                {
                    foreach (var item in outputs["ResolveAssemblyReferences"])
                    {
                        var reference = new MetaDataReference { Assembly = item.ItemSpec };

                        foreach (string metaData in item.MetadataNames)
                        {
                            var metaDataObj = new ProjectTaskMetaData { Name = metaData.Replace("\0", ""), Value = item.GetMetadata(metaData).Replace("\0", "") };

                            reference.MetaData.Add(metaDataObj);
                        }

                        metaDataReferences.Add(reference);
                    }
                }
                string targetPath = null;
                if (outputs.TryGetValue("GetTargetPath", out var targetPathOutput) && targetPathOutput.Length > 0)
                    targetPath = targetPathOutput[0].ItemSpec;
                    
                

                return new MsBuildProjectInformation
                {
                    MetaDataReferences = metaDataReferences,
                    ProjectReferences = projectReferences,
                    TargetPath = targetPath
                };
            }
            else
            {
                Console.WriteLine("Project load failed.");

                return null;
            }
        }

        public Task<string> GetVersion()
        {
            return Task.FromResult("1.02");
        }

        public void Shutdown()
        {
            _serverCompleted.SetResult(true);
        }

        public Task<List<string>> GetTargetFrameworks(string projectFile)
        {
            var document = XDocument.Load(projectFile);

            var targetFramework = document.Descendants("TargetFramework").FirstOrDefault();
            var targetFrameworks = document.Descendants("TargetFrameworks");

            var result = new List<string>();

            if (targetFramework != null)
            {
                result.Add(targetFramework.Value);
            }

            if (targetFrameworks != null)
            {
                foreach (var framework in targetFrameworks)
                {
                    var frameworks = framework.Value.Split(';');

                    foreach (var value in frameworks)
                    {
                        result.Add(value);
                    }
                }
            }

            return Task.FromResult(result);
        }
    }

    public class AvalonStudioTask : ITask
    {
        private static int FreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private static Task<bool> StartSever(IBuildEngine buildEngine)
        {
            var router = new DefaultTargetSelector();

            var host = new MSBuildHostService(buildEngine);
            var result = host.ServerTask;

            router.Register<IMsBuildHostService, MSBuildHostService>(host);

            var tcpHost = new TcpHost(new Engine().CreateRequestHandler(router));

            result.ContinueWith(_ => { tcpHost.StopListening(); });

            var port = FreeTcpPort();

            tcpHost.StartListening(new System.Net.IPEndPoint(IPAddress.Loopback, port));

            Console.WriteLine($"AvalonStudio MSBuild Host Started: {port}");

            return result;
        }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            StartSever(BuildEngine).Wait();

            return true;
        }
    }
}
