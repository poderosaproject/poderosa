// Copyright 2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace Poderosa.Plugin.Remoting {

    /// <summary>
    /// Service interface
    /// </summary>
    [ServiceContract]
    public interface IPoderosaRemotingService {
        /// <summary>
        /// Open shortcut file.
        /// </summary>
        /// <param name="path">path of the shortcut file</param>
        /// <returns>true if opened. otherwise false.</returns>
        [OperationContract]
        bool OpenShortcutFile(string path);
    }

    /// <summary>
    /// Methods for the service host
    /// </summary>
    public static class PoderosaRemotingServiceHost {

        private static ServiceHost _serviceHost;

        /// <summary>
        /// Starts service host.
        /// </summary>
        /// <param name="serviceInstance">singleton service instance</param>
        /// <returns>true if succeeded.</returns>
        public static bool Start(IPoderosaRemotingService serviceInstance) {
            if (_serviceHost != null) {
                return true;
            }

            var baseUri = new Uri(GetBaseAddress(Process.GetCurrentProcess().Id));
            var serviceHost = new ServiceHost(serviceInstance, baseUri);
            serviceHost.AddServiceEndpoint(typeof(IPoderosaRemotingService), new NetNamedPipeBinding(), "");

            try {
                serviceHost.Open();
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                return false;
            }

            _serviceHost = serviceHost;
            return true;
        }

        /// <summary>
        /// Shutdowns service host.
        /// </summary>
        public static void Shutdown() {
            var serviceHost = Interlocked.Exchange(ref _serviceHost, null);
            if (serviceHost == null) {
                return;
            }

            try {
                serviceHost.Close();
            }
            catch (Exception e) {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Gets base address of the service host.
        /// </summary>
        /// <param name="processId">process ID</param>
        /// <returns>base address</returns>
        internal static string GetBaseAddress(int processId) {
            return "net.pipe://localhost/poderosa." + VersionInfo.PODEROSA_VERSION + "/" + processId.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Mathods for the service client
    /// </summary>
    public static class PoderosaRemotingServiceClient {

        /// <summary>
        /// An action executed by <see cref="ForEachHost"/>.
        /// </summary>
        /// <param name="service">service proxy</param>
        /// <returns>true if tries the next service host.</returns>
        public delegate bool ForEachHostAction(IPoderosaRemotingService service);

        /// <summary>
        /// Finds other service hosts and execute action.
        /// </summary>
        /// <param name="action">action to execute</param>
        /// <returns>true if succeeded. false if something goes wrong.</returns>
        public static void ForEachHost(ForEachHostAction action) {
            try {
                foreach (var procId in FindPoderosaProcesses()) {
                    var address = PoderosaRemotingServiceHost.GetBaseAddress(procId);
                    var channelFactory = new ChannelFactory<IPoderosaRemotingService>(new NetNamedPipeBinding(), address);
                    var proxy = channelFactory.CreateChannel();

                    bool tryNext;
                    try {
                        tryNext = action(proxy);
                    }
                    catch (EndpointNotFoundException) {
                        continue;
                    }

                    channelFactory.Close();

                    if (!tryNext) {
                        break;
                    }
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
        /// Finds other Poderosa processes.
        /// </summary>
        /// <returns>process IDs</returns>
        private static int[] FindPoderosaProcesses() {
            var currentProcess = Process.GetCurrentProcess();
            var targetProcessName1 = currentProcess.ProcessName;
            // for debugging in VS
            string targetProcessName2;
            if (targetProcessName1.EndsWith(".vshost")) {
                targetProcessName2 = targetProcessName1.Substring(0, targetProcessName1.Length - 7);
            }
            else {
                targetProcessName2 = targetProcessName1 + ".vshost";
            }

            return Process.GetProcesses()
                .Where(process =>
                    process.Id != currentProcess.Id &&
                    (process.ProcessName == targetProcessName1 || process.ProcessName == targetProcessName2))
                .OrderBy(process => process.StartTime)
                .Select(process => process.Id)
                .ToArray();
        }
    }
}
