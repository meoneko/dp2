﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Web;
using System.IO;
using System.Diagnostics;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Marc;
using DigitalPlatform.CirculationClient.localhost;
using DigitalPlatform.Text;
using DigitalPlatform.CommonControl;

namespace dp2Circulation
{
    /// <summary>
    /// 册窗 / 订购窗 / 期窗 / 评注窗
    /// </summary>
    public partial class ItemInfoForm : MyForm
    {
        // 
        /// <summary>
        /// 数据库类型
        /// </summary>
        string m_strDbType = "item";  // comment order issue

        /// <summary>
        /// 数据库类型。为 item / order / issue / comment 之一
        /// </summary>
        public string DbType
        {
            get
            {
                return this.m_strDbType;
            }
            set
            {
                this.m_strDbType = value;

                if (this.m_strDbType == "comment")
                    this.toolStripButton_addSubject.Visible = true;
                else
                    this.toolStripButton_addSubject.Visible = false;

                this.Text = this.DbTypeCaption;
                this.comboBox_from.Items.Clear();   // 促使更换
            }
        }

        /// <summary>
        /// 当前已经装载的记录路径
        /// </summary>
        public string ItemRecPath = ""; // 当前已经装载的册记录路径
        /// <summary>
        /// 当前已装载的书目记录路径
        /// </summary>
        public string BiblioRecPath = "";   // 当前已装载的书目记录路径

        const int WM_LOAD_RECORD = API.WM_USER + 200;
        const int WM_PREV_RECORD = API.WM_USER + 201;
        const int WM_NEXT_RECORD = API.WM_USER + 202;

        Commander commander = null;
        WebExternalHost m_webExternalHost_item = new WebExternalHost();
        WebExternalHost m_webExternalHost_biblio = new WebExternalHost();

#if NO
        public LibraryChannel Channel = new LibraryChannel();
        public string Lang = "zh";

        /// <summary>
        /// 框架窗口
        /// </summary>
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        /// <summary>
        /// 构造函数
        /// </summary>
        public ItemInfoForm()
        {
            InitializeComponent();
        }

        private void ItemInfoForm_Load(object sender, EventArgs e)
        {
            if (this.MainForm != null)
            {
                MainForm.SetControlFont(this, this.MainForm.DefaultFont);
            }
#if NO
            MainForm.AppInfo.LoadMdiChildFormStates(this,
    "mdi_form_state");
            this.Channel.Url = this.MainForm.LibraryServerUrl;

            this.Channel.BeforeLogin -= new BeforeLoginEventHandle(Channel_BeforeLogin);
            this.Channel.BeforeLogin += new BeforeLoginEventHandle(Channel_BeforeLogin);

            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// 和容器关联
#endif

            // webbrowser
            this.m_webExternalHost_item.Initial(this.MainForm, this.webBrowser_itemHTML);
            this.webBrowser_itemHTML.ObjectForScripting = this.m_webExternalHost_item;

            this.m_webExternalHost_biblio.Initial(this.MainForm, this.webBrowser_biblio);
            this.webBrowser_biblio.ObjectForScripting = this.m_webExternalHost_biblio;

            this.commander = new Commander(this);
            this.commander.IsBusy -= new IsBusyEventHandler(commander_IsBusy);
            this.commander.IsBusy += new IsBusyEventHandler(commander_IsBusy);

            this.Text = this.DbTypeCaption;
        }

        void commander_IsBusy(object sender, IsBusyEventArgs e)
        {
            e.IsBusy = this.m_webExternalHost_item.ChannelInUse || this.m_webExternalHost_biblio.ChannelInUse;
        }
#if NO
        void Channel_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            this.MainForm.Channel_BeforeLogin(this, e);
        }
#endif

        private void ItemInfoForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (stop != null)
            {
                if (stop.State == 0)    // 0 表示正在处理
                {
                    MessageBox.Show(this, "请在关闭窗口前停止正在进行的长时操作。");
                    e.Cancel = true;
                    return;
                }

            }
#endif
        }

        private void ItemInfoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.commander.Destroy();

            if (this.m_webExternalHost_item != null)
                this.m_webExternalHost_item.Destroy();
            if (this.m_webExternalHost_biblio != null)
                this.m_webExternalHost_biblio.Destroy();

#if NO
            if (stop != null) // 脱离关联
            {
                stop.Unregister();	// 和容器关联
                stop = null;
            }
            MainForm.AppInfo.SaveMdiChildFormStates(this,
   "mdi_form_state");
#endif
        }

        /*
        void SetXmlToWebbrowser(WebBrowser webbrowser,
            string strXml)
        {
            string strTargetFileName = MainForm.DataDir + "\\xml.xml";

            StreamWriter sw = new StreamWriter(strTargetFileName,
                false,	// append
                System.Text.Encoding.UTF8);
            sw.Write(strXml);
            sw.Close();

            webbrowser.Navigate(strTargetFileName);
        }
         * */

        /// <summary>
        /// 重新装载当前记录
        /// </summary>
        /// <returns>-1: 出错; 1: 成功</returns>
        public int Reload()
        {
            return LoadRecordByRecPath(this.ItemRecPath, "");
        }

        // 
        /// <summary>
        /// 根据册条码号，装入册记录和书目记录
        /// 本方式只能当 DbType 为 "item" 时调用
        /// </summary>
        /// <param name="strItemBarcode">册条码号</param>
        /// <returns>-1: 出错; 1: 成功</returns>
        public int LoadRecord(string strItemBarcode)
        {
            Debug.Assert(this.m_strDbType == "item", "");

            string strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("正在初始化浏览器组件 ...");
            stop.BeginLoop();


            this.Update();
            this.MainForm.Update();


            Global.ClearHtmlPage(this.webBrowser_itemHTML,
                this.MainForm.DataDir);
            Global.ClearHtmlPage(this.webBrowser_itemXml,
                this.MainForm.DataDir);
            Global.ClearHtmlPage(this.webBrowser_biblio,
                this.MainForm.DataDir);
            // this.textBox_message.Text = "";
            this.toolStripLabel_message.Text = "";

            stop.SetMessage("正在装入册记录 " + strItemBarcode + " ...");


            try
            {
                string strItemText = "";
                string strBiblioText = "";

                string strItemRecPath = "";
                string strBiblioRecPath = "";

                byte[] item_timestamp = null;

                long lRet = Channel.GetItemInfo(
                    stop,
                    strItemBarcode,
                    "html",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1 || lRet == 0)
                    goto ERROR1;

                this.ItemRecPath = strItemRecPath;    // 2009/10/18
                this.BiblioRecPath = strBiblioRecPath;  // 2013/3/4

                if (lRet > 1)
                {
                    this.textBox_queryWord.Text = strItemBarcode;
                    this.comboBox_from.Text = "册条码";

                    strError = "册条码号 '" + strItemBarcode + "' 检索命中" + lRet.ToString() + " 条册记录，它们的路径如下：" + strItemRecPath + "；装入操作被放弃。\r\n\r\n这是一个严重的错误，请尽快联系系统管理员解决此问题。\r\n\r\n如要装入其中的任何一条，请采用记录路径方式装入。";
                    goto ERROR1;
                }

#if NO
                Global.SetHtmlString(this.webBrowser_itemHTML,
                    strItemText,
                    this.MainForm.DataDir,
                    "iteminfoform_item");
#endif
                this.m_webExternalHost_item.SetHtmlString(strItemText,
                    "iteminfoform_item");

                if (String.IsNullOrEmpty(strBiblioText) == true)
                    Global.SetHtmlString(this.webBrowser_biblio,
                        "(书目记录 '" + strBiblioRecPath + "' 不存在)");
                else
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_biblio,
                        strBiblioText,
                        this.MainForm.DataDir,
                        "iteminfoform_biblio");
#endif
                    this.m_webExternalHost_biblio.SetHtmlString(strBiblioText,
                        "iteminfoform_biblio");
                }

                // this.textBox_message.Text = "册记录路径: " + strItemRecPath + " ；其从属的种(书目)记录路径: " + strBiblioRecPath;
                this.toolStripLabel_message.Text = this.DbTypeCaption + "记录路径: " + strItemRecPath + " ；其从属的种(书目)记录路径: " + strBiblioRecPath;

                this.textBox_queryWord.Text = strItemBarcode;
                this.comboBox_from.Text = "册条码号";

                // 最后获得item xml
                lRet = Channel.GetItemInfo(
                    stop,
                    strItemBarcode,
                    "xml",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    null,   // "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                if (lRet == -1 || lRet == 0)
                {
                    Global.SetHtmlString(this.webBrowser_itemXml,
                        HttpUtility.HtmlEncode(strError));
                }
                else
                {
                    /*
                    SetXmlToWebbrowser(this.webBrowser_itemXml,
                        strItemText);
                     * */
                    // 把 XML 字符串装入一个Web浏览器控件
                    // 这个函数能够适应"<root ... />"这样的没有prolog的XML内容
                    Global.SetXmlToWebbrowser(this.webBrowser_itemXml,
                        this.MainForm.DataDir,
                        "xml",
                        strItemText);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);

                this.textBox_queryWord.SelectAll();
                this.textBox_queryWord.Focus();
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        /// <summary>
        /// 数据库类型的显示名称
        /// </summary>
        public string DbTypeCaption
        {
            get
            {
                if (this.m_strDbType == "item")
                    return "册";
                else if (this.m_strDbType == "comment")
                    return "评注";
                else if (this.m_strDbType == "order")
                    return "订购";
                else if (this.m_strDbType == "issue")
                    return "期";
                else
                    throw new Exception("未知的DbType '" + this.m_strDbType + "'");
            }
        }

        // 
        /// <summary>
        /// 根据册/订购/期/评注记录路径，装入事项记录和书目记录
        /// </summary>
        /// <param name="strItemRecPath">事项记录路径</param>
        /// <param name="strPrevNextStyle">前后翻动风格</param>
        /// <returns>-1: 出错; 1: 成功</returns>
        public int LoadRecordByRecPath(string strItemRecPath,
            string strPrevNextStyle)
        {
            string strError = "";

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("正在初始化浏览器组件 ...");
            stop.BeginLoop();

            this.Update();
            this.MainForm.Update();

            bool bPrevNext = false;

            string strRecPath = strItemRecPath;

            // 2009/10/18
            if (String.IsNullOrEmpty(strPrevNextStyle) == false)
            {
                strRecPath += "$" + strPrevNextStyle.ToLower();
                bPrevNext = true;
            }

            if (bPrevNext == false)
            {
                Global.ClearHtmlPage(this.webBrowser_itemHTML,
                    this.MainForm.DataDir);
                Global.ClearHtmlPage(this.webBrowser_itemXml,
                    this.MainForm.DataDir);
                Global.ClearHtmlPage(this.webBrowser_biblio,
                    this.MainForm.DataDir);
                // this.textBox_message.Text = "";
                this.toolStripLabel_message.Text = "";
            }

            stop.SetMessage("正在装入"+this.DbTypeCaption+"记录 " + strItemRecPath + " ...");


            try
            {
                string strItemText = "";
                string strBiblioText = "";

                string strOutputItemRecPath = "";
                string strBiblioRecPath = "";

                byte[] item_timestamp = null;

                string strBarcode = "@path:" + strRecPath;

                long lRet = 0;
                
                if (this.m_strDbType == "item")
                lRet = Channel.GetItemInfo(
                     stop,
                     strBarcode,
                     "html",
                     out strItemText,
                     out strOutputItemRecPath,
                     out item_timestamp,
                     "html",
                     out strBiblioText,
                     out strBiblioRecPath,
                     out strError);
                else if (this.m_strDbType == "comment")
                    lRet = Channel.GetCommentInfo(
                         stop,
                         strBarcode,    // "@path:" + strItemRecPath,
                         // "",
                         "html",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "html",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "order")
                    lRet = Channel.GetOrderInfo(
                         stop,
                         strBarcode,    // "@path:" + strItemRecPath,
                         // "",
                         "html",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "html",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "issue")
                    lRet = Channel.GetIssueInfo(
                         stop,
                         strBarcode,    // "@path:" + strItemRecPath,
                         // "",
                         "html",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "html",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else
                    throw new Exception("未知的DbType '" + this.m_strDbType + "'");


                if (lRet == -1 || lRet == 0)
                {


                    if (bPrevNext == true
                        && this.Channel.ErrorCode == DigitalPlatform.CirculationClient.localhost.ErrorCode.NotFound)
                    {
                        strError += "\r\n\r\n新记录没有装载，窗口中还保留了装载前的记录";
                        goto ERROR1;
                    }


                    this.ItemRecPath = strOutputItemRecPath;    // 2011/9/5
                    this.BiblioRecPath = strBiblioRecPath;  // 2013/3/4
#if NO
                    Global.SetHtmlString(this.webBrowser_itemHTML,
    strError,
    this.MainForm.DataDir,
    "iteminfoform_item");
#endif
                    this.m_webExternalHost_item.SetHtmlString(strError,
    "iteminfoform_item");

                }
                else
                {
                    this.ItemRecPath = strOutputItemRecPath;    // 2009/10/18
                    this.BiblioRecPath = strBiblioRecPath;  // 2013/3/4

#if NO
                    Global.SetHtmlString(this.webBrowser_itemHTML,
                        strItemText,
                        this.MainForm.DataDir,
                        "iteminfoform_item");
#endif
                    this.m_webExternalHost_item.SetHtmlString(strItemText,
                        "iteminfoform_item");
                }

                if (String.IsNullOrEmpty(strBiblioText) == true)
                    Global.SetHtmlString(this.webBrowser_biblio,
                        "(书目记录 '" + strBiblioRecPath + "' 不存在)");
                else
                {
#if NO
                    Global.SetHtmlString(this.webBrowser_biblio,
                        strBiblioText,
                        this.MainForm.DataDir,
                        "iteminfoform_biblio");
#endif
                    this.m_webExternalHost_biblio.SetHtmlString(strBiblioText,
                        "iteminfoform_biblio");
                }

                // this.textBox_message.Text = "册记录路径: " + strOutputItemRecPath + " ；其从属的种(书目)记录路径: " + strBiblioRecPath;
                this.toolStripLabel_message.Text = this.DbTypeCaption+"记录路径: " + strOutputItemRecPath + " ；其从属的种(书目)记录路径: " + strBiblioRecPath;
                this.textBox_queryWord.Text = this.ItemRecPath; // strItemRecPath;
                this.comboBox_from.Text = this.DbTypeCaption+"记录路径";

                // 最后获得item xml
                if (this.m_strDbType == "item")
                lRet = Channel.GetItemInfo(
                    stop,
                    "@path:" + strOutputItemRecPath, // strBarcode,
                    "xml",
                    out strItemText,
                    out strItemRecPath,
                    out item_timestamp,
                    null,   // "html",
                    out strBiblioText,
                    out strBiblioRecPath,
                    out strError);
                else if (this.m_strDbType == "comment")
                    lRet = Channel.GetCommentInfo(
                         stop,
                         "@path:" + strOutputItemRecPath,
                         // "",
                         "xml",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "order")
                    lRet = Channel.GetOrderInfo(
                         stop,
                         "@path:" + strOutputItemRecPath,
                         // "",
                         "xml",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else if (this.m_strDbType == "issue")
                    lRet = Channel.GetIssueInfo(
                         stop,
                         "@path:" + strOutputItemRecPath,
                         // "",
                         "xml",
                         out strItemText,
                         out strOutputItemRecPath,
                         out item_timestamp,
                         "",
                         out strBiblioText,
                         out strBiblioRecPath,
                         out strError);
                else
                    throw new Exception("未知的DbType '" + this.m_strDbType + "'");


                if (lRet == -1 || lRet == 0)
                {
                    Global.SetHtmlString(this.webBrowser_itemXml,
                        HttpUtility.HtmlEncode(strError));
                }
                else
                {
                    /*
                    SetXmlToWebbrowser(this.webBrowser_itemXml,
                        strItemText);
                     * */
                    // 把 XML 字符串装入一个Web浏览器控件
                    // 这个函数能够适应"<root ... />"这样的没有prolog的XML内容
                    Global.SetXmlToWebbrowser(this.webBrowser_itemXml,
                        this.MainForm.DataDir,
                        "xml",
                        strItemText);
                }
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            return 1;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

#if NO
        void DoStop(object sender, StopEventArgs e)
        {
            if (this.Channel != null)
                this.Channel.Abort();
        }
#endif

        void SetMenuItemState()
        {
            // 菜单

            // 工具条按钮

            this.MainForm.MenuItem_recoverUrgentLog.Enabled = false;
            this.MainForm.MenuItem_font.Enabled = false;
            this.MainForm.MenuItem_restoreDefaultFont.Enabled = false;

            this.MainForm.toolButton_refresh.Enabled = true;
        }

        private void ItemInfoForm_Activated(object sender, EventArgs e)
        {
            this.MainForm.stopManager.Active(this.stop);

            SetMenuItemState();
        }

        /// <summary>
        /// 缺省窗口过程
        /// </summary>
        /// <param name="m">消息</param>
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOAD_RECORD:
                    this.toolStrip1.Enabled = false;
                    try
                    {
                        if (this.m_webExternalHost_item.CanCallNew(
                            this.commander,
                            m.Msg) == true
                            && this.m_webExternalHost_biblio.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            DoLoadRecord();
                        }
                    }
                    finally
                    {
                        this.toolStrip1.Enabled = true;
                    }
                    return;
                case WM_PREV_RECORD:
                    this.toolStrip1.Enabled = false;
                    try
                    {
                        if (this.m_webExternalHost_item.CanCallNew(
                            this.commander,
                            m.Msg) == true
                            && this.m_webExternalHost_biblio.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            LoadRecordByRecPath(this.ItemRecPath, "prev");
                        }
                    }
                    finally
                    {
                        this.toolStrip1.Enabled = true;
                    }
                    return;
                case WM_NEXT_RECORD:
                    this.toolStrip1.Enabled = false;
                    try
                    {
                        if (this.m_webExternalHost_item.CanCallNew(
                            this.commander,
                            m.Msg) == true
                            && this.m_webExternalHost_biblio.CanCallNew(
                            this.commander,
                            m.Msg) == true)
                        {
                            LoadRecordByRecPath(this.ItemRecPath, "next");
                        }
                    }
                    finally
                    {
                        this.toolStrip1.Enabled = true;
                    }
                    return;
            }
            base.DefWndProc(ref m);
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            if (this.textBox_queryWord.Text == "")
            {
                MessageBox.Show(this, "尚未输入检索词");
                return;
            }

            this.toolStrip1.Enabled = false;
            this.button_load.Enabled = false;

            this.m_webExternalHost_item.StopPrevious();
            this.webBrowser_itemHTML.Stop();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_LOAD_RECORD);
        }

        private void DoLoadRecord()
        {
            string strError;
            if (this.textBox_queryWord.Text == "")
            {
                strError = "尚未输入检索词";
                goto ERROR1;
            }

            if (this.comboBox_from.Text == "册条码"
                || this.comboBox_from.Text == "册条码号")
            {
                if (this.m_strDbType != "item")
                {
                    strError = "只能当DbType为item时才能使用 册条码号 检索途径";
                    goto ERROR1;
                }
                int nRet = this.textBox_queryWord.Text.IndexOf("/");
                if (nRet != -1)
                {
                    strError = "您输入的检索词似乎为一个记录路径，而不是册条码号";
                    MessageBox.Show(this, strError);
                }

                LoadRecord(this.textBox_queryWord.Text);
            }
            else if (this.comboBox_from.Text == this.DbTypeCaption + "记录路径")
            {
                int nRet = this.textBox_queryWord.Text.IndexOf("/");
                if (nRet == -1)
                {
                    strError = "您输入的检索词似乎为一个册条码号，而不是"+this.DbTypeCaption+"记录路径";
                    MessageBox.Show(this, strError);
                }

                // LoadRecord("@path:" + this.textBox_queryWord.Text);
                LoadRecordByRecPath(this.textBox_queryWord.Text, "");
            }
            else
            {
                strError = "无法识别的检索途径 '" + this.comboBox_from.Text + "'";
                goto ERROR1;
            }

            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        /// <summary>
        /// 允许或者禁止界面控件。在长操作前，一般需要禁止界面控件；操作完成后再允许
        /// </summary>
        /// <param name="bEnable">是否允许界面控件。true 为允许， false 为禁止</param>
        public override void EnableControls(bool bEnable)
        {
            this.comboBox_from.Enabled = bEnable;
            this.textBox_queryWord.Enabled = bEnable;
            this.button_load.Enabled = bEnable;
            this.toolStrip1.Enabled = bEnable;  // 避免使用工具条上的命令按钮
        }

        private void toolStripButton_prevRecord_Click(object sender, EventArgs e)
        {
            this.toolStrip1.Enabled = false;
            this.button_load.Enabled = false;

            this.m_webExternalHost_item.StopPrevious();
            this.webBrowser_itemHTML.Stop();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_PREV_RECORD);
        }

        private void toolStripButton_nextRecord_Click(object sender, EventArgs e)
        {
            this.toolStrip1.Enabled = false;
            this.button_load.Enabled = false;

            this.m_webExternalHost_item.StopPrevious();
            this.webBrowser_itemHTML.Stop();

            this.m_webExternalHost_biblio.StopPrevious();
            this.webBrowser_biblio.Stop();

            this.commander.AddMessage(WM_NEXT_RECORD);

        }

        private void comboBox_from_DropDown(object sender, EventArgs e)
        {
            this.comboBox_from.Items.Clear();

            if (this.m_strDbType == "item")
                this.comboBox_from.Items.Add("册条码号");

            this.comboBox_from.Items.Add(this.DbTypeCaption + "记录路径");
        }

        // 增添自由词
        private void toolStripButton_addSubject_Click(object sender, EventArgs e)
        {
            string strError = "";
            int nRet = 0;

            EnableControls(false);

            stop.OnStop += new StopEventHandler(this.DoStop);
            stop.Initial("正在获取书目记录 ...");
            stop.BeginLoop();

            try
            {
                List<string> reserve_subjects = null;
                List<string> exist_subjects = null;
                byte[] biblio_timestamp = null;
                string strBiblioXml = "";

                nRet = GetExistSubject(
                    this.BiblioRecPath,
                    out strBiblioXml,
                    out reserve_subjects,
                    out exist_subjects,
                    out biblio_timestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;

                string strCommentState = "";
                string strNewSubject = "";
                byte[] item_timestamp = null;
                nRet = GetCommentContent(this.ItemRecPath,
            out strNewSubject,
            out strCommentState,
            out item_timestamp,
            out strError);
                if (nRet == -1)
                    goto ERROR1;

                AddSubjectDialog dlg = new AddSubjectDialog();
                MainForm.SetControlFont(dlg, this.Font, false);
                dlg.ReserveSubjects = reserve_subjects;
                dlg.ExistSubjects = exist_subjects;
                dlg.HiddenNewSubjects = StringUtil.SplitList(strNewSubject.Replace("\\r", "\n"), '\n');
                if (StringUtil.IsInList("已处理", strCommentState) == false)
                    dlg.NewSubjects = dlg.HiddenNewSubjects;

                this.MainForm.AppInfo.LinkFormState(dlg, "iteminfoform_addsubjectdialog_state");
                dlg.ShowDialog(this);
                this.MainForm.AppInfo.UnlinkFormState(dlg);

                if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return;

                List<string> subjects = new List<string>();
                subjects.AddRange(dlg.ExistSubjects);
                subjects.AddRange(dlg.NewSubjects);

                StringUtil.RemoveDupNoSort(ref subjects);   // 去重
                StringUtil.RemoveBlank(ref subjects);   // 去掉空元素

                // 修改指示符1为空的那些 610 字段
                // parameters:
                //      strSubject  可以修改的自由词的总和。包括以前存在的和本次添加的
                nRet = ChangeSubject(ref strBiblioXml,
                    subjects,
                    out strError);

                // 保存书目记录
                byte[] output_timestamp = null;
                string strOutputBiblioRecPath = "";
                long lRet = Channel.SetBiblioInfo(
                    stop,
                    "change",
                    this.BiblioRecPath,
                    "xml",
                    strBiblioXml,
                    biblio_timestamp,
                    "",
                    out strOutputBiblioRecPath,
                    out output_timestamp,
                    out strError);
                if (lRet == -1)
                    goto ERROR1;

                // 修改评注记录状态
                // return:
                //       -1  出错
                //      0   没有发生修改
                //      1   发生了修改
                nRet = ChangeCommentState(
                    this.BiblioRecPath,
                    this.ItemRecPath,
                    "已处理",
                    "",
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }
            finally
            {
                stop.EndLoop();
                stop.OnStop -= new StopEventHandler(this.DoStop);
                stop.Initial("");

                EnableControls(true);
            }

            // 重新装载内容
            this.Reload();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        // 修改评注的状态
        // return:
        //       -1  出错
        //      0   没有发生修改
        //      1   发生了修改
        /// <summary>
        /// 修改评注记录的状态字段
        /// </summary>
        /// <param name="strBiblioRecPath">书目记录路径</param>
        /// <param name="strCommentRecPath">评注记录路径</param>
        /// <param name="strAddList">要在状态字符串中加入的子串列表</param>
        /// <param name="strRemoveList">要在状态字符串中删除的子串列表</param>
        /// <param name="strError">返回出错信息</param>
        /// <returns>
        ///       -1  出错
        ///      0   没有发生修改
        ///      1   发生了修改
        /// </returns>
        public int ChangeCommentState(
            string strBiblioRecPath,
            string strCommentRecPath,
            string strAddList,
            string strRemoveList,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (String.IsNullOrEmpty(strCommentRecPath) == true)
            {
                strError = "CommentRecPath为空";
                goto ERROR1;
            }

            if (String.IsNullOrEmpty(strBiblioRecPath) == true)
            {
                strError = "strBiblioRecPath为空";
                goto ERROR1;
            }

            // 获得旧记录
            string strOldXml = "";
            // byte[] timestamp = ByteArray.GetTimeStampByteArray(this.Timestamp);

            string strOutputPath = "";
            byte[] comment_timestamp = null;
            string strBiblio = "";
            string strTempBiblioRecPath = "";
            long lRet = Channel.GetCommentInfo(
null,
"@path:" + strCommentRecPath,
"xml", // strResultType
out strOldXml,
out strOutputPath,
out comment_timestamp,
"recpath",  // strBiblioType
out strBiblio,
out strTempBiblioRecPath,
out strError);
            if (lRet == -1)
            {
                strError = "获得原有评注记录 '" + strCommentRecPath + "' 时出错: " + strError;
                goto ERROR1;
            }

#if NO
            if (ByteArray.Compare(comment_timestamp, timestamp) != 0)
            {
                strError = "修改被拒绝。因为记录 '" + strCommentRecPath + "' 在保存前已经被其他人修改过。请重新装载";
                goto ERROR1;
            }
#endif


            XmlDocument dom = new XmlDocument();
            if (String.IsNullOrEmpty(strOldXml) == false)
            {
                try
                {
                    dom.LoadXml(strOldXml);
                }
                catch (Exception ex)
                {
                    strError = "装载记录XML进入DOM时发生错误: " + ex.Message;
                    goto ERROR1;
                }
            }
            else
                dom.LoadXml("<root/>");

            // 仅仅修改状态
            {
                string strState = DomUtil.GetElementText(dom.DocumentElement,
                    "state");
                string strOldState = strState;

                Global.ModifyStateString(ref strState,
    strAddList,
    strRemoveList);

                if (strState == strOldState)
                    return 0;   // 没有必要修改

                DomUtil.SetElementText(dom.DocumentElement,
                    "state", strState);

                // 在<operations>中写入适当条目
                string strComment = "'" + strOldState + "' --> '" + strState + "'";
                nRet = Global.SetOperation(
                    ref dom,
                    "stateModified",
                    Channel.UserName,
                    strComment,
                    true,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            string strNewCommentRecPath = "";
            string strNewXml = "";
            byte[] baNewTimestamp = null;

            {
                strNewCommentRecPath = strCommentRecPath;

                // 覆盖
                nRet = ChangeCommentInfo(
                    strBiblioRecPath,
                    strCommentRecPath,
                    strOldXml,
                    dom.DocumentElement.OuterXml,
                    comment_timestamp,
                    out strNewXml,
                    out baNewTimestamp,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
            }

            return 1;
        ERROR1:
            return -1;
        }

        // 
        /// <summary>
        /// 修改一个评注记录
        /// </summary>
        /// <param name="strBiblioRecPath">书目记录路径</param>
        /// <param name="strCommentRecPath">评注记录路径</param>
        /// <param name="strOldXml">评注记录修改前的 XML</param>
        /// <param name="strCommentXml">评注记录要修改成的 XML</param>
        /// <param name="timestamp">修改前的时间戳</param>
        /// <param name="strNewXml">返回实际保存成功的评注记录 XML</param>
        /// <param name="baNewTimestamp">返回修改后的时间戳</param>
        /// <param name="strError">返回出错信息</param>
        /// <returns>-1: 出错; 1: 成功</returns>
        public int ChangeCommentInfo(
            string strBiblioRecPath,
            string strCommentRecPath,
            string strOldXml,
            string strCommentXml,
            byte[] timestamp,
            out string strNewXml,
            out byte[] baNewTimestamp,
            out string strError)
        {
            strError = "";
            strNewXml = "";
            baNewTimestamp = null;

            EntityInfo info = new EntityInfo();
            info.RefID = Guid.NewGuid().ToString();

            string strTargetBiblioRecID = Global.GetRecordID(strBiblioRecPath);

            XmlDocument comment_dom = new XmlDocument();
            try
            {
                comment_dom.LoadXml(strCommentXml);
            }
            catch (Exception ex)
            {
                strError = "XML装载到DOM时发生错误: " + ex.Message;
                return -1;
            }

            DomUtil.SetElementText(comment_dom.DocumentElement,
                "parent", strTargetBiblioRecID);

            info.Action = "change";
            info.OldRecPath = strCommentRecPath;
            info.NewRecPath = strCommentRecPath;
            info.OldRecord = strOldXml;
            info.OldTimestamp = timestamp;
            info.NewRecord = comment_dom.OuterXml;
            info.NewTimestamp = null;

            // 
            EntityInfo[] comments = new EntityInfo[1];
            comments[0] = info;

            EntityInfo[] errorinfos = null;

            long lRet = Channel.SetComments(
                null,
                strBiblioRecPath,
                comments,
                out errorinfos,
                out strError);
            if (lRet == -1)
            {
                return -1;
            }

            if (errorinfos != null && errorinfos.Length > 0)
            {
                int nErrorCount = 0;
                for (int i = 0; i < errorinfos.Length; i++)
                {
                    EntityInfo error = errorinfos[i];
                    if (error.ErrorCode != ErrorCodeValue.NoError)
                    {
                        if (String.IsNullOrEmpty(strError) == false)
                            strError += "; ";
                        strError += errorinfos[0].ErrorInfo;
                        nErrorCount++;
                    }
                    else
                    {
                        // strNewCommentRecPath = error.NewRecPath;
                        strNewXml = error.NewRecord;
                        baNewTimestamp = error.NewTimestamp;
                    }
                }
                if (nErrorCount > 0)
                {
                    return -1;
                }
            }

            return 1;
        }

        /// <summary>
        /// 获得评注记录内容
        /// 请参考 dp2Library API GetCommentInfo() 的详细信息
        /// </summary>
        /// <param name="strCommentRecPath">评注记录路径</param>
        /// <param name="strContent">返回内容</param>
        /// <param name="strState">返回状态</param>
        /// <param name="item_timestamp">返回时间戳</param>
        /// <param name="strError">返回出错信息</param>
        /// <returns>-1: 出错; 0: 成功</returns>
        int GetCommentContent(string strCommentRecPath,
            out string strContent,
            out string strState,
            out byte[] item_timestamp,
            out string strError)
        {
            strError = "";
            strContent = "";
            strState = "";
            item_timestamp = null;

            string strCommentXml = "";
            string strOutputItemRecPath = "";
            string strBiblioText = "";
            string strBiblioRecPath = "";
            long lRet = Channel.GetCommentInfo(
     stop,
     "@path:" + strCommentRecPath,
                // "",
     "xml",
     out strCommentXml,
     out strOutputItemRecPath,
     out item_timestamp,
     null,
     out strBiblioText,
     out strBiblioRecPath,
     out strError);
            if (lRet == -1)
                return -1;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strCommentXml);
            }
            catch (Exception ex)
            {
                strError = "XML装入DOM时间出错: " + ex.Message;
                return -1;
            }

            strState = DomUtil.GetElementText(dom.DocumentElement, "state");
            strContent = DomUtil.GetElementText(dom.DocumentElement, "content");
            return 0;
        }

        // 修改指示符1为空的那些 610 字段
        // parameters:
        //      subjects  可以修改的自由词的总和。包括以前存在的和本次添加的
        static int ChangeSubject(ref string strBiblioXml,
            List<string> subjects,
            out string strError)
        {
            strError = "";

            // 对主题词去重

            string strMARC = "";
            string strMarcSyntax = "";
            // 将XML格式转换为MARC格式
            // 自动从数据记录中获得MARC语法
            int nRet = MarcUtil.Xml2Marc(strBiblioXml,
                true,
                null,
                out strMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
            {
                strError = "XML转换到MARC记录时出错: " + strError;
                return -1;
            }

            nRet = ChangeSubject(ref strMARC,
                strMarcSyntax,
                subjects,
                out strError);
            if (nRet == -1)
                return -1;

            nRet = MarcUtil.Marc2XmlEx(strMARC,
                strMarcSyntax,
                ref strBiblioXml,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        /// <summary>
        /// 根据提供的主题词字符串 修改 MARC 记录中的 610 或 653 字段
        /// </summary>
        /// <param name="strMARC">要操作的 MARC 记录字符串。机内格式</param>
        /// <param name="strMarcSyntax">MARC 格式</param>
        /// <param name="subjects">主题词字符串集合</param>
        /// <param name="strError">返回出错信息</param>
        /// <returns>-1: 出错; 0: 成功</returns>
        public static int ChangeSubject(ref string strMARC,
            string strMarcSyntax,
            List<string> subjects,
            out string strError)
        {
            strError = "";

            MarcRecord record = new MarcRecord(strMARC);
            MarcNodeList nodes = null;
            if (strMarcSyntax == "unimarc")
                nodes = record.select("field[@name='610' and @indicator1=' ']");
            else if (strMarcSyntax == "usmarc")
                nodes = record.select("field[@name='653' and @indicator1=' ']");
            else
            {
                strError = "未知的 MARC 格式类型 '" + strMarcSyntax + "'";
                return -1;
            }

            if (subjects == null || subjects.Count == 0)
            {
                // 删除那些可以删除的 610 字段
                foreach (MarcNode node in nodes)
                {
                    MarcNodeList subfields = node.select("subfield[@name='a']");
                    if (subfields.count == node.ChildNodes.count)
                    {
                        // 如果除了 $a 以外没有其他任何子字段，则字段可以删除
                        node.detach();
                    }
                }
            }
            else
            {

                MarcNode field610 = null;

                // 只留下一个 610 字段
                if (nodes.count > 1)
                {
                    int nCount = nodes.count;
                    foreach (MarcNode node in nodes)
                    {
                        MarcNodeList subfields = node.select("subfield[@name='a']");
                        if (subfields.count == node.ChildNodes.count)
                        {
                            // 如果除了 $a 以外没有其他任何子字段，则字段可以删除
                            node.detach();
                            nCount--;
                        }

                        if (nCount <= 1)
                            break;
                    }

                    // 重新选定
                    if (strMarcSyntax == "unimarc")
                        nodes = record.select("field[@name='610' and @indicator1=' ']");
                    else if (strMarcSyntax == "usmarc")
                        nodes = record.select("field[@name='653' and @indicator1=' ']");

                    field610 = nodes[0];
                }
                else if (nodes.count == 0)
                {
                    // 创建一个新的 610 字段
                    if (strMarcSyntax == "unimarc")
                        field610 = new MarcField("610", "  ");
                    else if (strMarcSyntax == "usmarc")
                        field610 = new MarcField("653", "  ");

                    record.ChildNodes.insertSequence(field610);
                }
                else
                {
                    Debug.Assert(nodes.count == 1, "");
                    field610 = nodes[0];
                }

                // 删除全部 $a 子字段
                field610.select("subfield[@name='a']").detach();


                // 添加若干个 $a 子字段
                Debug.Assert(subjects.Count > 0, "");
                MarcNodeList source = new MarcNodeList();
                for (int i = 0; i < subjects.Count; i++)
                {
                    source.add(new MarcSubfield("a", subjects[i]));
                }
                // 寻找适当位置插入
                field610.ChildNodes.insertSequence(source[0]);
                if (source.count > 1)
                {
                    // 在刚插入的对象后面插入其余的对象
                    MarcNodeList list = new MarcNodeList(source[0]);
                    source.removeAt(0); // 排除刚插入的一个
                    list.after(source);
                }
            }

            strMARC = record.Text;
            return 0;
        }

        // parameters:
        //      reserve_subjects   保留的自由词。指指示符1为 0/1/2 的自由词。这些自由词不让对话框修改(可以在 MARC 编辑器修改)
        //      subjects          让修改的自由词。指示符1为 空。这些自由词让对话框修改
        int GetExistSubject(
            string strBiblioRecPath,
            out string strBiblioXml,
            out List<string> reserve_subjects,
            out List<string> subjects,
            out byte [] timestamp,
            out string strError)
        {
            strError = "";
            reserve_subjects = new List<string>();
            subjects = new List<string>();
            timestamp = null;
            strBiblioXml = "";

            string[] results = null;

            // 获得书目记录
            long lRet = Channel.GetBiblioInfos(
                stop,
                strBiblioRecPath,
                "",
                new string[] { "xml" },   // formats
                out results,
                out timestamp,
                out strError);
            if (lRet == 0)
                return -1;
            if (lRet == -1)
                return -1;

            if (results == null || results.Length == 0)
            {
                strError = "results error";
                return -1;
            }

            strBiblioXml = results[0];

            string strMARC = "";
            string strMarcSyntax = "";
            // 将XML格式转换为MARC格式
            // 自动从数据记录中获得MARC语法
            int nRet = MarcUtil.Xml2Marc(strBiblioXml,
                true,
                null,
                out strMarcSyntax,
                out strMARC,
                out strError);
            if (nRet == -1)
            {
                strError = "XML转换到MARC记录时出错: " + strError;
                return -1;
            }

            nRet = GetSubjectInfo(strMARC,
                strMarcSyntax,
                out reserve_subjects,
                out subjects,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        /// <summary>
        /// 从 MARC 字符串中获得主题词信息
        /// </summary>
        /// <param name="strMARC">MARC 字符串。机内格式</param>
        /// <param name="strMarcSyntax">MARC 格式</param>
        /// <param name="reserve_subjects">返回要保留的主题词集合。字段指示符1 不为空的</param>
        /// <param name="subjects">返回主题词集合。字段指示符1 为空的</param>
        /// <param name="strError">返回出错信息</param>
        /// <returns>-1: 出错; 0: 成功</returns>
        public static int GetSubjectInfo(string strMARC,
            string strMarcSyntax,
            out List<string> reserve_subjects,
            out List<string> subjects,
            out string strError)
        {
            strError = "";
            reserve_subjects = new List<string>();
            subjects = new List<string>();

            MarcRecord record = new MarcRecord(strMARC);
            MarcNodeList nodes = null;
            if (strMarcSyntax == "unimarc")
                nodes = record.select("field[@name='610']/subfield[@name='a']");
            else if (strMarcSyntax == "usmarc")
                nodes = record.select("field[@name='653']/subfield[@name='a']");
            else
            {
                strError = "未知的 MARC 格式类型 '" + strMarcSyntax + "'";
                return -1;
            }

            foreach (MarcNode node in nodes)
            {
                if (string.IsNullOrEmpty(node.Content.Trim()) == true)
                    continue;

                Debug.Assert(node.NodeType == NodeType.Subfield, "");

                if (node.Parent.Indicator1 == ' ')
                    subjects.Add(node.Content.Trim());
                else
                    reserve_subjects.Add(node.Content.Trim());
            }

            return 0;
        }
    }
}