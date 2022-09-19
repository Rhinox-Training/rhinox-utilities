using Newtonsoft.Json.Linq;

namespace RMDY.Networking
{
    public interface IMessageTransformer
    {
        void Transform(JContainer json);
    }
}