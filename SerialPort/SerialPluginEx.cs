/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SerialPluginEx.cs,v 1.3 2012/03/18 11:02:30 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using Poderosa.Util;

namespace Poderosa.SerialPort {
    //シリアルに必要なEnum
    /// <summary>
    /// <ja>フローコントロールの設定</ja>
    /// <en>Specifies the flow control.</en>
    /// </summary>
    /// <exclude/>
    public enum FlowControl {
        /// <summary>
        /// <ja>なし</ja>
        /// <en>None</en>
        /// </summary>
        [EnumValue(Description = "Enum.FlowControl.None")]
        None,
        /// <summary>
        /// X ON / X OFf
        /// </summary>
        [EnumValue(Description = "Enum.FlowControl.Xon_Xoff")]
        Xon_Xoff,
        /// <summary>
        /// <ja>ハードウェア</ja>
        /// <en>Hardware</en>
        /// </summary>
        [EnumValue(Description = "Enum.FlowControl.Hardware")]
        Hardware
    }

    /// <summary>
    /// <ja>パリティの設定</ja>
    /// <en>Specifies the parity.</en>
    /// </summary>
    /// <exclude/>
    public enum Parity {
        /// <summary>
        /// <ja>なし</ja>
        /// <en>None</en>
        /// </summary>
        [EnumValue(Description = "Enum.Parity.NOPARITY")]
        NOPARITY = 0,
        /// <summary>
        /// <ja>奇数</ja>
        /// <en>Odd</en>
        /// </summary>
        [EnumValue(Description = "Enum.Parity.ODDPARITY")]
        ODDPARITY = 1,
        /// <summary>
        /// <ja>偶数</ja>
        /// <en>Even</en>
        /// </summary>
        [EnumValue(Description = "Enum.Parity.EVENPARITY")]
        EVENPARITY = 2
        //MARKPARITY  =        3,
        //SPACEPARITY =        4
    }

    /// <summary>
    /// <ja>ストップビットの設定</ja>
    /// <en>Specifies the stop bits.</en>
    /// </summary>
    /// <exclude/>
    public enum StopBits {
        /// <summary>
        /// <ja>1ビット</ja>
        /// <en>1 bit</en>
        /// </summary>
        [EnumValue(Description = "Enum.StopBits.ONESTOPBIT")]
        ONESTOPBIT = 0,
        /// <summary>
        /// <ja>1.5ビット</ja>
        /// <en>1.5 bits</en>
        /// </summary>
        [EnumValue(Description = "Enum.StopBits.ONE5STOPBITS")]
        ONE5STOPBITS = 1,
        /// <summary>
        /// <ja>2ビット</ja>
        /// <en>2 bits</en>
        /// </summary>
        [EnumValue(Description = "Enum.StopBits.TWOSTOPBITS")]
        TWOSTOPBITS = 2
    }
}
