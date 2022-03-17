using System.Collections.Generic;

namespace Rhinox.Utilities
{
    public interface IConfigFile
    {
        void Initialize();
        ICollection<ConfigField> FindFields();
    }
}