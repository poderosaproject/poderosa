
using Granados.SSH1;
using Granados.SSH2;
using Granados.Util;
using System;
using System.Diagnostics.Tracing;

namespace Granados.SSH {

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


}
