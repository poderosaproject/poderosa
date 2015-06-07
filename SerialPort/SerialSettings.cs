/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SerialSettings.cs,v 1.6 2012/03/15 15:19:18 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

using Poderosa.Util;
using Poderosa.Serializing;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Terminal;
using Poderosa.View;
using Poderosa.MacroEngine;

namespace Poderosa.SerialPort {
    internal class SerialTerminalParam : ITerminalParameter, IAutoExecMacroParameter {
        private string _portName;
        private string _terminalType;
        private string _autoExecMacro;

        [MacroConnectionParameter]
        public string PortName {
            get {
                return _portName;
            }
            set {
                _portName = value;
            }
        }

        public SerialTerminalParam() {
            _portName = "COM1";
        }

        //シリアルでは幅・高さは関知せず。しかしこの値は最初のGLineの長さにもなるので０にはできない
        public int InitialWidth {
            get {
                return 80;
            }
        }

        public int InitialHeight {
            get {
                return 25;
            }
        }

        public string TerminalType {
            get {
                return _terminalType;
            }
        }

        public void SetTerminalName(string terminalType) {
            _terminalType = terminalType;
        }
        public void SetTerminalSize(int width, int height) {
            //do nothing
        }

        public bool UIEquals(ITerminalParameter param) {
            SerialTerminalParam tp = param as SerialTerminalParam;
            return tp != null && _portName == tp.PortName;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return SerialPortPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public object Clone() {
            SerialTerminalParam tp = new SerialTerminalParam();
            tp._portName = _portName;
            tp._terminalType = _terminalType;
            return tp;
        }

        #region IAutoExecMacroParameter

        public string AutoExecMacroPath {
            get {
                return _autoExecMacro;
            }
            set {
                _autoExecMacro = value;
            }
        }

        #endregion
    }

    internal class SerialTerminalSettings : TerminalSettings {

        private int _baudRate;
        private byte _byteSize;  //7,8のどちらか
        private Parity _parity; //Win32クラス内の定数のいずれか
        private StopBits _stopBits; //Win32クラス内の定数のいずれか
        private FlowControl _flowControl;
        private int _transmitDelayPerChar;
        private int _transmitDelayPerLine;

        /// <summary>
        /// <ja>デフォルト設定で初期化します。</ja>
        /// <en>Initializes with default values.</en>
        /// <seealso cref="Poderosa.Macro.ConnectionList.Open"/>
        /// </summary>
        /// <remarks>
        /// <ja>パラメータは次のように初期化されます。</ja>
        /// <en>The parameters are set as following:</en>
        /// <list type="table">
        ///   <item><term><ja>エンコーディング</ja><en>Encoding</en></term><description><ja>EUC-JP</ja><en>iso-8859-1</en></description></item>　
        ///   <item><term><ja>ログ</ja><en>Log</en></term><description><ja>取得しない</ja><en>None</en></description></item>　　　　　　　
        ///   <item><term><ja>ローカルエコー</ja><en>Local echo</en></term><description><ja>しない</ja><en>Don't</en></description></item>　　
        ///   <item><term><ja>送信時改行</ja><en>New line</en></term><description>CR</description></item>　　　　
        ///   <item><term><ja>ボーレート</ja><en>Baud Rate</en></term><description>9600</description></item>
        ///   <item><term><ja>データ</ja><en>Data Bits</en></term><description><ja>8ビット</ja><en>8 bits</en></description></item>
        ///   <item><term><ja>パリティ</ja><en>Parity</en></term><description><ja>なし</ja><en>None</en></description></item>
        ///   <item><term><ja>ストップビット</ja><en>Stop Bits</en></term><description><ja>１ビット</ja><en>1 bit</en></description></item>
        ///   <item><term><ja>フローコントロール</ja><en>Flow Control</en></term><description><ja>なし</ja><en>None</en></description></item>
        /// </list>
        /// <ja>接続を開くには、<see cref="Poderosa.Macro.ConnectionList.Open"/>メソッドの引数としてSerialTerminalParamオブジェクトを渡します。</ja>
        /// <en>To open a new connection, pass the SerialTerminalParam object to the <see cref="Poderosa.Macro.ConnectionList.Open"/> method.</en>
        /// </remarks>
        public SerialTerminalSettings() {
            _baudRate = 9600;
            _byteSize = 8;
            _parity = Parity.NOPARITY;
            _stopBits = StopBits.ONESTOPBIT;
            _flowControl = FlowControl.None;
        }
        public override ITerminalSettings Clone() {
            SerialTerminalSettings p = new SerialTerminalSettings();
            p.Import(this);
            return p;
        }

        public void BaseImport(ITerminalSettings ts) {
            base.Import(ts);
            //アイコンは保持する
            this.BeginUpdate();
            this.Icon = SerialPortPlugin.Instance.LoadIcon();
            this.EndUpdate();
        }

        public override void Import(ITerminalSettings src) {
            base.Import(src);
            SerialTerminalSettings p = src as SerialTerminalSettings;
            Debug.Assert(p != null);

            _baudRate = p._baudRate;
            _byteSize = p._byteSize;
            _parity = p._parity;
            _stopBits = p._stopBits;
            _flowControl = p._flowControl;
            _transmitDelayPerChar = p._transmitDelayPerChar;
            _transmitDelayPerLine = p._transmitDelayPerLine;
        }



        //TODO 以下ではEnsureUpdateでなくていいのか？

        /// <summary>
        /// <ja>ボーレートです。</ja>
        /// <en>Gets or sets the baud rate.</en>
        /// </summary>
        [MacroConnectionParameter]
        public int BaudRate {
            get {
                return _baudRate;
            }
            set {
                _baudRate = value;
            }
        }
        /// <summary>
        /// <ja>データのビット数です。</ja>
        /// <en>Gets or sets the bit count of the data.</en>
        /// </summary>
        /// <remarks>
        /// <ja>７か８でないといけません。</ja>
        /// <en>The value must be 7 or 8.</en>
        /// </remarks>
        [MacroConnectionParameter]
        public byte ByteSize {
            get {
                return _byteSize;
            }
            set {
                _byteSize = value;
            }
        }
        /// <summary>
        /// <ja>パリティです。</ja>
        /// <en>Gets or sets the parity.</en>
        /// </summary>
        [MacroConnectionParameter]
        public Parity Parity {
            get {
                return _parity;
            }
            set {
                _parity = value;
            }
        }
        /// <summary>
        /// <ja>ストップビットです。</ja>
        /// <en>Gets or sets the stop bit.</en>
        /// </summary>
        [MacroConnectionParameter]
        public StopBits StopBits {
            get {
                return _stopBits;
            }
            set {
                _stopBits = value;
            }
        }
        /// <summary>
        /// <ja>フローコントロールです。</ja>
        /// <en>Gets or sets the flow control.</en>
        /// </summary>
        [MacroConnectionParameter]
        public FlowControl FlowControl {
            get {
                return _flowControl;
            }
            set {
                _flowControl = value;
            }
        }

        /// <summary>
        /// <ja>文字あたりのディレイ(ミリ秒単位)です。</ja>
        /// <en>Gets or sets the delay time per a character in milliseconds.</en>
        /// </summary>
        public int TransmitDelayPerChar {
            get {
                return _transmitDelayPerChar;
            }
            set {
                _transmitDelayPerChar = value;
            }
        }
        /// <summary>
        /// <ja>行あたりのディレイ(ミリ秒単位)です。</ja>
        /// <en>Gets or sets the delay time per a line in milliseconds.</en>
        /// </summary>
        public int TransmitDelayPerLine {
            get {
                return _transmitDelayPerLine;
            }
            set {
                _transmitDelayPerLine = value;
            }
        }

    }

    //Serializers
    internal class SerialTerminalParamSerializer : ISerializeServiceElement {
        public Type ConcreteType {
            get {
                return typeof(SerialTerminalParam);
            }
        }


        public StructuredText Serialize(object obj) {
            SerialTerminalParam tp = obj as SerialTerminalParam;
            Debug.Assert(tp != null);

            StructuredText node = new StructuredText(this.ConcreteType.FullName);
            node.Set("PortName", tp.PortName);
            if (tp.TerminalType != "vt100")
                node.Set("TerminalType", tp.TerminalType);
            if (tp.AutoExecMacroPath != null)
                node.Set("autoexec-macro", tp.AutoExecMacroPath);
            return node;
        }

        public object Deserialize(StructuredText node) {
            SerialTerminalParam tp = new SerialTerminalParam();
            if (node.Get("Port") != null) {
                // accept old parameter.
                // "PortName" setting overwrites this setting.
                tp.PortName = "COM" + node.Get("Port");
            }
            tp.PortName = node.Get("PortName", tp.PortName);
            tp.SetTerminalName(node.Get("TerminalType", "vt100"));
            tp.AutoExecMacroPath = node.Get("autoexec-macro", null);
            return tp;
        }
    }
    internal class SerialTerminalSettingsSerializer : ISerializeServiceElement {
        public Type ConcreteType {
            get {
                return typeof(SerialTerminalSettings);
            }
        }


        public StructuredText Serialize(object obj) {
            SerialTerminalSettings ts = obj as SerialTerminalSettings;
            Debug.Assert(ts != null);

            StructuredText node = new StructuredText(this.ConcreteType.FullName);
            node.AddChild(SerialPortPlugin.Instance.SerializeService.Serialize(typeof(TerminalSettings), ts));

            node.Set("baud-rate", ts.BaudRate.ToString());
            if (ts.ByteSize != 8)
                node.Set("byte-size", ts.ByteSize.ToString());
            if (ts.Parity != Parity.NOPARITY)
                node.Set("parity", ts.Parity.ToString());
            if (ts.StopBits != StopBits.ONESTOPBIT)
                node.Set("stop-bits", ts.StopBits.ToString());
            if (ts.FlowControl != FlowControl.None)
                node.Set("flow-control", ts.FlowControl.ToString());
            if (ts.TransmitDelayPerChar != 0)
                node.Set("delay-per-char", ts.TransmitDelayPerChar.ToString());
            if (ts.TransmitDelayPerLine != 0)
                node.Set("delay-per-line", ts.TransmitDelayPerLine.ToString());

            return node;
        }

        public object Deserialize(StructuredText node) {
            SerialTerminalSettings ts = SerialPortUtil.CreateDefaultSerialTerminalSettings("COM1");

            //TODO Deserializeの別バージョンを作ってimportさせるべきだろう。もしくはService側の実装から変える。要素側には空引数コンストラクタを強制すればいいか
            StructuredText basenode = node.FindChild(typeof(TerminalSettings).FullName);
            if (basenode != null)
                ts.BaseImport((ITerminalSettings)SerialPortPlugin.Instance.SerializeService.Deserialize(basenode));

            ts.BaudRate = ParseUtil.ParseInt(node.Get("baud-rate"), 9600);
            ts.ByteSize = (byte)ParseUtil.ParseInt(node.Get("byte-size"), 8);
            ts.Parity = ParseUtil.ParseEnum<Parity>(node.Get("parity"), Parity.NOPARITY);
            ts.StopBits = ParseUtil.ParseEnum<StopBits>(node.Get("stop-bits"), StopBits.ONESTOPBIT);
            ts.FlowControl = ParseUtil.ParseEnum<FlowControl>(node.Get("flow-control"), FlowControl.None);
            ts.TransmitDelayPerChar = ParseUtil.ParseInt(node.Get("delay-per-char"), 0);
            ts.TransmitDelayPerLine = ParseUtil.ParseInt(node.Get("delay-per-line"), 0);

            return ts;
        }
    }
}
