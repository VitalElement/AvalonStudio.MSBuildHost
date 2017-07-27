using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AvalonStudio.MSBuildHost
{
    public interface IMsBuildHostService
    {
        Task<Version> GetVersion();
    }
}
