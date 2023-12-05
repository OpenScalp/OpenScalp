using System.IO.Pipelines;
using System.Net;

namespace OpenScalp.QuikSharp;

public interface ITransport : IDuplexPipe
{
    IPEndPoint RemoteEndPoint { get; }
}