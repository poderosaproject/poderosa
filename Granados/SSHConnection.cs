
using Granados.IO;
using Granados.PortForwarding;
using Granados.SSH;

namespace Granados {

    /// <summary>
    /// Channel type
    /// </summary>
    public enum ChannelType {
        Session,
        Shell,
        ForwardedLocalToRemote,
        ForwardedRemoteToLocal,
        ExecCommand,
        Subsystem,
        AgentForwarding,
        Other,
    }

    /// <summary>
    /// SSH protocol event listener 
    /// </summary>
    public interface ISSHProtocolEventListener {

        /// <summary>
        /// Notifies sending a packet related to the negotiation or status changes.
        /// </summary>
        /// <param name="messageType">message type (message name defined in the SSH protocol specification)</param>
        /// <param name="details">text that describes details</param>
        void OnSend(string messageType, string details);

        /// <summary>
        /// Notifies a packet related to the negotiation or status changes has been received.
        /// </summary>
        /// <param name="messageType">message type (message name defined in the SSH protocol specification)</param>
        /// <param name="details">text that describes details</param>
        void OnReceived(string messageType, string details);

        /// <summary>
        /// Notifies additional informations related to the negotiation or status changes.
        /// </summary>
        /// <param name="details">text that describes details</param>
        void OnTrace(string details);
    }

    /// <summary>
    /// A proxy class for reading status of the underlying <see cref="IGranadosSocket"/> object.
    /// </summary>
    public class SocketStatusReader {

        private readonly IGranadosSocket _socket;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket object</param>
        internal SocketStatusReader(IGranadosSocket socket) {
            _socket = socket;
        }

        /// <summary>
        /// Gets status of the socket object.
        /// </summary>
        public SocketStatus SocketStatus {
            get {
                return _socket.SocketStatus;
            }
        }

        /// <summary>
        /// Gets whether any received data are available on the socket
        /// </summary>
        public bool DataAvailable {
            get {
                return _socket.DataAvailable;
            }
        }

    }

    /// <summary>
    /// SSH connection
    /// </summary>
    public interface ISSHConnection {

        /// <summary>
        /// SSH protocol (SSH1 or SSH2)
        /// </summary>
        SSHProtocol SSHProtocol {
            get;
        }

        /// <summary>
        /// Connection parameter
        /// </summary>
        SSHConnectionParameter ConnectionParameter {
            get;
        }

        /// <summary>
        /// A property that indicates whether this connection is open.
        /// </summary>
        bool IsOpen {
            get;
        }

        /// <summary>
        /// A proxy object for reading status of the underlying <see cref="IGranadosSocket"/> object.
        /// </summary>
        SocketStatusReader SocketStatusReader {
            get;
        }

        /// <summary>
        /// Sends a disconnect message to the server, then closes this connection.
        /// </summary>
        /// <param name="message">a message to be notified to the server</param>
        void Disconnect(string message);

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <remarks>
        /// This method closes the underlying socket object.
        /// </remarks>
        void Close();

        /// <summary>
        /// Opens shell channel (SSH2) or interactive session (SSH1)
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/></returns>
        THandler OpenShell<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator)
                where THandler : ISSHChannelEventHandler;

        /// <summary>
        /// Opens execute-command channel
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <param name="command">command to execute</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/></returns>
        THandler ExecCommand<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string command)
                where THandler : ISSHChannelEventHandler;

        /// <summary>
        /// Opens subsystem channel (SSH2 only)
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <param name="subsystemName">subsystem name</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/>.</returns>
        THandler OpenSubsystem<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string subsystemName)
                where THandler : ISSHChannelEventHandler;

        /// <summary>
        /// Opens local port forwarding channel
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <param name="remoteHost">the host to connect to</param>
        /// <param name="remotePort">the port number to connect to</param>
        /// <param name="originatorIp">originator's IP address</param>
        /// <param name="originatorPort">originator's port number</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/>.</returns>
        THandler ForwardPort<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string remoteHost, uint remotePort, string originatorIp, uint originatorPort)
                where THandler : ISSHChannelEventHandler;

        /// <summary>
        /// Requests the remote port forwarding.
        /// </summary>
        /// <param name="requestHandler">a handler that handles the port forwarding requests from the server</param>
        /// <param name="addressToBind">address to bind on the server</param>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <returns>true if the request has been accepted, otherwise false.</returns>
        bool ListenForwardedPort(IRemotePortForwardingHandler requestHandler, string addressToBind, uint portNumberToBind);

        /// <summary>
        /// Cancel the remote port forwarding. (SSH2 only)
        /// </summary>
        /// <param name="addressToBind">address to bind on the server</param>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <returns>true if the remote port forwarding has been cancelled, otherwise false.</returns>
        bool CancelForwardedPort(string addressToBind, uint portNumberToBind);

        /// <summary>
        /// Sends ignorable data
        /// </summary>
        /// <param name="message">a message to be sent. the server may record this message into the log.</param>
        void SendIgnorableData(string message);

    }

}

