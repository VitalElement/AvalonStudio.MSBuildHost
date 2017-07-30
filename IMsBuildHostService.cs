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

        Task<MsBuildHostServiceResponse<List<string>>> GetAssemblyReferences(string projectFile);

        Task<MsBuildHostServiceResponse<List<string>>> GetProjectFrameworks(string projectFile);

        Task<MsBuildHostServiceResponse<List<string>>> GetProjectReferences(string projectFile);

        Task<MsBuildHostServiceResponse<TaskItems>> GetTaskItem(string target, string projectFile, List<Property> properties);
    }
}
