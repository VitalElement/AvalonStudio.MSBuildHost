using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvalonStudio.MSBuildHost
{
    public class Property
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public interface IMsBuildHostService
    {
        Task<string> GetVersion();

        /// <summary>
        /// Loads a project in MSBuild and processes it without Compiling.
        /// The compile string will be output on the command line of the host.
        /// </summary>
        /// <param name="projectFile">.csproj file location</param>
        /// <param name="properties">properties to set during load.</param>
        /// <param name="targetFramework">the target framework to use.</param>
        /// <returns>A list of Assembly References.</returns>
        Task<(List<MetaDataReference> metaDataReferences, List<string> projectReferences)> LoadProject(string solutionDirectory, string projectFile, string targetFrameworks = null);

        /// <summary>
        /// Returns a list of all target frameworks described by the project file.
        /// </summary>
        /// <param name="projectFile"></param>
        /// <returns></returns>
        Task<List<string>> GetTargetFrameworks(string projectFile);

        void Shutdown();
    }
}
