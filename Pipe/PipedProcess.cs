/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipedProcess.cs,v 1.2 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

namespace Poderosa.Pipe {

    /// <summary>
    /// Container of the objects relating to a piped process.
    /// </summary>
    internal class PipedProcess : IDisposable {

        private bool disposed = false;
        private readonly Process _process;
        private readonly SafeFileHandle _stdInHandle;
        private readonly SafeFileHandle _stdOutHandle;
        private readonly SafeFileHandle _stdErrorHandle;

        public event EventHandler Exited;

        public PipedProcess(Process process, SafeFileHandle stdInHandle, SafeFileHandle stdOutHandle, SafeFileHandle stdErrorHandle) {
            _process = process;
            _stdInHandle = stdInHandle;
            _stdOutHandle = stdOutHandle;
            _stdErrorHandle = stdErrorHandle;

            _process.Exited += delegate(object sender, EventArgs e) {
                if (!disposed && Exited != null)
                    Exited(sender, e);
            };
            _process.EnableRaisingEvents = true;
        }

        #region IDisposable

        public void Dispose() {
            lock (this) {
                if (disposed)
                    return;
                disposed = true;
            }

            if (!_process.HasExited) {
                try {
                    _process.Kill();
                }
                catch (Exception) {
                }
            }
            _stdErrorHandle.Dispose();
            _stdOutHandle.Dispose();
            _stdInHandle.Dispose();
        }

        #endregion
    }

}
