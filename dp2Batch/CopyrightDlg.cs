﻿using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;

namespace dp2Batch
{
    /// <summary>
    /// Summary description for CopyrightDlg.
    /// </summary>
    public class CopyrightDlg : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button button_OK;
        private System.Windows.Forms.Label label_copyright;
        private System.Windows.Forms.TextBox textBox_environment;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public CopyrightDlg()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            // this.Opacity = 0.75;

        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CopyrightDlg));
            this.label1 = new System.Windows.Forms.Label();
            this.label_copyright = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.button_OK = new System.Windows.Forms.Button();
            this.textBox_environment = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(5, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(454, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "dp2批处理 dp2batch V2.1 (2012/4/4)";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label_copyright
            // 
            this.label_copyright.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label_copyright.Location = new System.Drawing.Point(5, 52);
            this.label_copyright.Name = "label_copyright";
            this.label_copyright.Size = new System.Drawing.Size(454, 34);
            this.label_copyright.TabIndex = 1;
            this.label_copyright.Text = "(C) 版权所有 2005-2011 数字平台(北京)软件有限责任公司\\r\\nDigital Platform (Beijing) Software Corp. " +
    "Ltd.";
            this.label_copyright.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel1.Location = new System.Drawing.Point(5, 86);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(454, 17);
            this.linkLabel1.TabIndex = 2;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "http://www.dp2003.com";
            this.linkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // button_OK
            // 
            this.button_OK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.button_OK.Location = new System.Drawing.Point(197, 277);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(70, 20);
            this.button_OK.TabIndex = 3;
            this.button_OK.Text = "确定";
            this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
            // 
            // textBox_environment
            // 
            this.textBox_environment.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_environment.Location = new System.Drawing.Point(5, 110);
            this.textBox_environment.Multiline = true;
            this.textBox_environment.Name = "textBox_environment";
            this.textBox_environment.ReadOnly = true;
            this.textBox_environment.Size = new System.Drawing.Size(454, 162);
            this.textBox_environment.TabIndex = 6;
            this.textBox_environment.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // CopyrightDlg
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(464, 303);
            this.Controls.Add(this.textBox_environment);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label_copyright);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CopyrightDlg";
            this.ShowInTaskbar = false;
            this.Text = "Copyright 版权";
            this.Load += new System.EventHandler(this.CopyrightDlg_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private void button_OK_Click(object sender, System.EventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                System.Threading.Thread.Sleep(10);

                this.Opacity = (double)1 - ((double)i / (double)100);
                this.Update();
                //Application.DoEvents();
            }

            Close();
        }

        private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            /*
			// Determine which link was clicked within the LinkLabel.
			MessageBox.Show(linkLabel1.Links[0].ToString());
			return;
			*/
            System.Diagnostics.Process.Start(linkLabel1.Text);

        }

        private void CopyrightDlg_Load(object sender, System.EventArgs e)
        {

            label_copyright.Text = "(C) 版权所有 2005-2011 数字平台(北京)软件有限责任公司\r\nDigital Platform (Beijing) Software Corp. Ltd.";

            Assembly myAssembly;

            myAssembly = Assembly.GetAssembly(this.GetType());
            textBox_environment.Text = "本机 .NET Framework 版本: " + myAssembly.ImageRuntimeVersion
                + "\r\ndp2Batch: " + myAssembly.FullName;


            /*
			for(int i = 0; i<100; i++) 
			{
				System.Threading.Thread.Sleep(10);

				this.Opacity = ((double)(i+1)/(double)100);
				this.Update();

			}
			*/

        }
    }
}
