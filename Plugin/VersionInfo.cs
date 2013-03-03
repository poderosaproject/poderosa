/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: VersionInfo.cs,v 1.13 2012/06/09 15:06:58 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Poderosa {
    /// <summary>
    /// <ja>
    /// バージョン情報を返します。
    /// </ja>
    /// <en>
    /// Return the version information.
    /// </en>
    /// </summary>
    /// <exclude/>
    public class VersionInfo {
        /// <summary>
        /// <ja>
        /// バージョン番号です。
        /// </ja>
        /// <en>
        /// Version number.
        /// </en>
        /// </summary>
        public const string PODEROSA_VERSION = "4.3.10b";
        /// <summary>
        /// <ja>
        /// リリース日です。
        /// </ja>
        /// <en>
        /// Release Date.
        /// </en>
        /// </summary>
        public const string RELEASE_DATE = "Nov 22, 2006";
        /// <summary>
        /// <ja>
        /// プロジェクト名です。
        /// </ja>
        /// <en>
        /// Project name.
        /// </en>
        /// </summary>
        public const string PROJECT_NAME = "Poderosa Project";
    }
}
