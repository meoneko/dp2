﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Threading;

using DigitalPlatform;
using DigitalPlatform.Interfaces;

namespace FingerprintCenter
{
    public class FingerprintServer : MarshalByRefObject, IFingerprint, IDisposable
    {
        public int GetVersion(out string strVersion,
            out string strCfgInfo,
            out string strError)
        {
            strVersion = "2.0";
            strCfgInfo = "selfInitCache";
            strError = "";
            return 0;
        }

        // return:
        //      -1  出错
        //      0   调用前端口已经打开
        //      1   成功
        public int Open(out string strError)
        {
            strError = "";

#if NO
            eventClose.Reset();

            if (this.m_host != null)
            {
                strError = "FingerprintServer 已经打开";
                return 0;
            }

            /*
System.PlatformNotSupportedException: 系统上未安装语音，或没有当前安全设置可用的语音。

Server stack trace: 
   在 System.Speech.Internal.Synthesis.VoiceSynthesis..ctor(WeakReference speechSynthesizer)
   在 System.Speech.Synthesis.SpeechSynthesizer.get_VoiceSynthesizer()
   在 System.Speech.Synthesis.SpeechSynthesizer.remove_SpeakCompleted(EventHandler`1 value)
   在 ZkFingerprint.FingerprintServer.Open(String& strError)
   在 System.Runtime.Remoting.Messaging.StackBuilderSink._PrivateProcessMessage(IntPtr md, Object[] args, Object server, Int32 methodPtr, Boolean fExecuteInContext, Object[]& outArgs)
   在 System.Runtime.Remoting.Messaging.StackBuilderSink.SyncProcessMessage(IMessage msg, Int32 methodPtr, Boolean fExecuteInContext)

Exception rethrown at [0]: 
   在 System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)
   在 System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type)
   在 DigitalPlatform.Interfaces.IFingerprint.Open(String& strError)
   在 ZkFingerprint.MainForm.OpenServer(Boolean bDisplayErrorMessage)
   在 ZkFingerprint.MainForm.MainForm_Load(Object sender, EventArgs e)
   在 System.Windows.Forms.Form.OnLoad(EventArgs e)
   在 System.Windows.Forms.Form.OnCreateControl()
   在 System.Windows.Forms.Control.CreateControl(Boolean fIgnoreVisible)
   在 System.Windows.Forms.Control.CreateControl()
   在 System.Windows.Forms.Control.WmShowWindow(Message& m)
   在 System.Windows.Forms.Control.WndProc(Message& m)
   在 System.Windows.Forms.ScrollableControl.WndProc(Message& m)
   在 System.Windows.Forms.Form.WmShowWindow(Message& m)
   在 System.Windows.Forms.Form.WndProc(Message& m)
   在 ZkFingerprint.MainForm.WndProc(Message& m)
   在 System.Windows.Forms.Control.ControlNativeWindow.OnMessage(Message& m)
   在 System.Windows.Forms.Control.ControlNativeWindow.WndProc(Message& m)
   在 System.Windows.Forms.NativeWindow.Callback(IntPtr hWnd, Int32 msg, IntPtr wparam, IntPtr lparam)

             * */
            try
            {
                this.m_speech = new SpeechSynthesizer();
                this.m_speech.SpeakCompleted -= new EventHandler<SpeakCompletedEventArgs>(m_speech_SpeakCompleted);
                this.m_speech.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(m_speech_SpeakCompleted);
            }
            catch (System.PlatformNotSupportedException ex)
            {
                strError = ex.Message;
                return -1;
            }

            try
            {
                m_host = new ZKFPEngX();
            }
            catch (Exception ex)
            {
                strError = "open error : " + ex.Message;
                return -1;
            }

            //  0 初始化成功
            //  1 指纹识别驱动程序加载失败
            //  2 没有连接指纹识别仪
            //  3 属性 SensorIndex 指定的指纹仪不存在
            int nRet = this.m_host.InitEngine();
            if (nRet != 0)
            {
                if (nRet == 1)
                    strError = "指纹识别驱动程序加载失败";
                else if (nRet == 2)
                    strError = "尚未连接指纹阅读器";
                else if (nRet == 3)
                    strError = "属性 SensorIndex (" + this.m_host.SensorIndex.ToString() + ") 指定的指纹仪不存在";
                else
                    strError = "初始化失败，错误码 " + nRet.ToString();

                Speak(strError);
                this.m_host = null;
                return -1;
            }

            this.m_host.FPEngineVersion = "10";

            this.m_host.OnFeatureInfo -= new IZKFPEngXEvents_OnFeatureInfoEventHandler(m_host_OnFeatureInfo);
            this.m_host.OnFeatureInfo += new IZKFPEngXEvents_OnFeatureInfoEventHandler(m_host_OnFeatureInfo);

            this.m_host.OnImageReceived -= new IZKFPEngXEvents_OnImageReceivedEventHandler(m_host_OnImageReceived);
            this.m_host.OnImageReceived += new IZKFPEngXEvents_OnImageReceivedEventHandler(m_host_OnImageReceived);

            this.m_host.OnCapture -= new IZKFPEngXEvents_OnCaptureEventHandler(m_host_OnCapture);
            this.m_host.OnCapture += new IZKFPEngXEvents_OnCaptureEventHandler(m_host_OnCapture);
            this.m_host.BeginCapture();

            Speak("指纹阅读器接口程序成功启动");
#endif
            return 1;
        }

        // 设置参数
        public bool SetParameter(string strName, object value)
        {
#if NO
            if (strName == "Threshold")
            {
                // 指纹识别系统比对识别阈值
                // 1-100 默认 10
                this.m_host.Threshold = (int)value;
                return true;
            }
            if (strName == "OneToOneThreshold")
            {
                // 低速指纹 1:1 比对的识别阈值分数
                // 1-100 默认 10
                this.m_host.Threshold = (int)value;
                return true;
            }
#endif
            return false;
        }

        public int Close()
        {
            return 1;
        }

        // 添加高速缓存事项
        // 如果items == null 或者 items.Count == 0，表示要清除当前的全部缓存内容
        // 如果一个item对象的FingerprintString为空，表示要删除这个缓存事项
        public int AddItems(List<FingerprintItem> items,
            out string strError)
        {
            return FingerPrint.AddItems(items,
    out strError);
        }

        public int CancelGetFingerprintString()
        {
            FingerPrint.CancelRegisterString();
            return 0;
        }

        // TODO: 防止函数过程重入
        // 获得一个指纹特征字符串
        // return:
        //      -1  error
        //      0   放弃输入
        //      1   成功输入
        public int GetFingerprintString(out string strFingerprintString,
            out string strVersion,
            out string strError)
        {
            strError = "";
            strFingerprintString = "";
            strVersion = "";

            try
            {
                TextResult result = FingerPrint.GetRegisterString();
                if (result.Value == -1)
                {
                    strError = result.ErrorInfo;
                    return -1;
                }

                strFingerprintString = result.Text;
                strVersion = "10";
                return 1;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return 0;
            }

#if NO
            if (this.m_host == null)
            {
                if (Open(out strError) == -1)
                    return -1;
            }

            // this.m_host.CancelCapture();

            m_bInRegister = true;
            ActivateMainForm(true);
            DisplayCancelButton(true);
            try
            {
                strVersion = "zk-" + this.m_host.FPEngineVersion;

                this.m_host.EnrollCount = 1;

                this.m_host.OnEnroll -= new IZKFPEngXEvents_OnEnrollEventHandler(m_host_OnEnroll);
                this.m_host.OnEnroll += new IZKFPEngXEvents_OnEnrollEventHandler(m_host_OnEnroll);

                eventFinished.Reset();
                m_bActionResult = false;
                m_bCanceled = false;

                this.m_host.BeginEnroll();

                string strText = "请扫描指纹。\r\n\r\n总共需要扫描 " + this.m_host.EnrollCount.ToString() + " 次";
                DisplayInfo(strText);

                Speak("请扫描指纹。一共需要按 " + this.m_host.EnrollCount.ToString() + " 次");

                WaitHandle[] events = new WaitHandle[2];

                events[0] = eventClose;
                events[1] = eventFinished;

                int index = WaitHandle.WaitAny(events, -1, false);

                if (index == WaitHandle.WaitTimeout)
                {
                    strError = "超时";
                    DisplayInfo(strError);
                    return -1;
                }
                else if (index == 0)
                {
                    strError = "接口被关闭";
                    DisplayInfo(strError);
                    return -1;
                }

                // 取消
                if (m_bCanceled == true)
                {
                    strError = "获取指纹信息的操作被取消";
                    DisplayInfo(strError);
                    Speak(strError);
                    return 0;
                }

                // 正常结束
                if (m_bActionResult == false)
                {
                    strError = "获取指纹信息失败";
                    DisplayInfo("非常抱歉，本轮获取指纹信息操作失败");
                    if (this.SpeakOn == false)
                        SafeBeep(3);
                    Speak("非常抱歉，本轮获取指纹信息操作失败");
                    return -1;
                }

                // strFingerprintString = this.m_host.GetTemplateAsStringEx("10");
                strFingerprintString = this.m_host.GetTemplateAsString();
                if (this.SpeakOn == false)
                    SafeBeep(1);

                DisplayInfo("获取指纹信息成功");
                Speak("指纹扫描完成。谢谢");
                return 1;
            }
            finally
            {
                // this.m_host.BeginCapture();
                DisplayCancelButton(false);
                ActivateMainForm(false);
                m_bInRegister = false;
            }
#endif
        }

        // 验证读者指纹. 1:1比对
        // parameters:
        //      item    读者信息。ReaderBarcode成员提供了读者证条码号，FingerprintString提供了指纹特征码
        //              如果 FingerprintString 不为空，则用它和当前采集的指纹进行比对；
        //              否则用 ReaderBarcode，对高速缓存中的指纹进行比对
        // return:
        //      -1  出错
        //      0   不匹配
        //      1   匹配
        public int VerifyFingerprint(FingerprintItem item,
            out string strError)
        {
            strError = "";

            return 0;

#if NO
            // 等到扫描一次指纹
            // 这次的扫描不要进行自动比对，也不要键盘仿真

            string strTemplate = item.FingerprintString;

            bool bRet = this.m_host.VerFingerFromStr(ref strTemplate,
                 strThisString,
                 false,
                 ref bChanged);
#endif
        }

        public void Dispose()
        {
            FingerPrint.CancelRegisterString();
        }
    }
}
