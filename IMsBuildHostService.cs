using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AvalonStudio.MSBuildHost
{
    public interface IMsBuildHostService
    {
        Task<string> GetVersion();

        Task<MsBuildHostServiceResponse<List<string>>> GetAssemblyReferences(string projectFile);

        Task<MsBuildHostServiceResponse<List<string>>> GetProjectReferences(string projectFile);

        Task<MsBuildHostServiceResponse<TaskItems>> GetTaskItem(string target, string projectFile);
    }
}
