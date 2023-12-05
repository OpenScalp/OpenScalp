using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace OpenScalp.QuikSharp;

public class SocketTransport : ITransport
{
    private readonly Pipe _outputPipe;
    private readonly Pipe _inputPipe;
    private readonly TaskCompletionSource _completion;

    public SocketTransport(Socket connectedSocket)
    {
        Socket = connectedSocket;
        RemoteEndPoint = (IPEndPoint)connectedSocket.RemoteEndPoint!;
        _outputPipe = new Pipe();
        _inputPipe = new Pipe();
        _completion = new TaskCompletionSource();

        PipelineToSocketAsync(_outputPipe.Reader, Socket);
        SocketToPipelineAsync(Socket, _inputPipe.Writer);
    }

    public Socket Socket { get; }
    public PipeReader Input => _inputPipe.Reader;
    public PipeWriter Output => _outputPipe.Writer;
    public IPEndPoint RemoteEndPoint { get; }

    private async void PipelineToSocketAsync(PipeReader pipeReader, Socket socket)
    {
        try
        {
            while (true)
            {
                var result = await pipeReader.ReadAsync();
                var buffer = result.Buffer;

                while (true)
                {
                    var memory = buffer.First;
                    if (memory.IsEmpty)
                        break;
                    var bytesSent = await socket.SendAsync(memory, SocketFlags.None);
                    buffer = buffer.Slice(bytesSent);
                    if (bytesSent != memory.Length)
                        break;
                }

                pipeReader.AdvanceTo(buffer.Start);

                if (result.IsCompleted)
                {
                    Close();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Close(ex);
        }
    }

    private async void SocketToPipelineAsync(Socket socket, PipeWriter pipeWriter)
    {
        try
        {
            while (true)
            {
                var buffer = pipeWriter.GetMemory();
                var bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
                if (bytesRead == 0) // Graceful close
                {
                    Close();
                    break;
                }

                pipeWriter.Advance(bytesRead);
                await pipeWriter.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            Close(ex);
        }
    }

    private void Close(Exception? exception = null)
    {
        if (exception != null)
        {
            if (_completion.TrySetException(exception))
                _inputPipe.Writer.Complete(exception);
        }
        else
        {
            if (_completion.TrySetResult())
                _inputPipe.Writer.Complete();
        }

        try
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }
        catch
        {
            // Ignore
        }
    }
}