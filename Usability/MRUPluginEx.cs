/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MRUPluginEx.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;

namespace Poderosa.Usability {
    /// <summary>
    /// Add this attribute if you don't want to save the session informations in the Most-Recently-Used list.
    /// </summary>
    /// <remarks>
    /// Use this attribute for the concrete class of the following interfaces.
    /// If one of these objects has <see cref="ExcludeFromMRUAttribute"/>, session information is not saved in MRU.
    /// <list type="bullet">
    ///  <item><description><see cref="Poderosa.Sessions.ITerminalSession"/></description></item>
    ///  <item><description><see cref="Poderosa.Protocols.ITerminalParameter"/></description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ExcludeFromMRUAttribute : Attribute {

        /// <summary>
        /// Constructor
        /// </summary>
        public ExcludeFromMRUAttribute() {
        }

    }

}
