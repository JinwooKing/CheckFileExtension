using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace CheckFileExtension
{
    public partial class Form1 : Form
    {
        public static string whiteList {get; set; } = string.Empty;
        public static string blackList { get; set; } = string.Empty;
        public static RichTextBox richTextBox { get; set; }


        public Form1()
        {
            InitializeComponent();
            richTextBox = richTextBox1;
        }

        //확장자 검사 button
        private void button1_Click(object sender, EventArgs e)
        {
            try { 
                string filePath = @textBox1.Text;
                whiteList = @textBox2.Text;
                blackList = @textBox3.Text;

                if (FileExtensionChk.CheckFileExtension(filePath))
                {                                                
                    WriteRichTextBoxText("업로드 가능합니다.");
                }
                else
                {
                    WriteRichTextBoxText("업로드 가능한 파일유형이 아닙니다.");
                }
            }
            catch (Exception exception)
            {
                WriteRichTextBoxText(exception.ToString());
            }
        }

        //변조 검사 button
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string filePath = @textBox1.Text;

                if (FileExtensionChk.CheckFileExtensionFromSignatures(filePath))
                {
                    WriteRichTextBoxText("업로드 가능합니다.");
                }
                else
                {
                    WriteRichTextBoxText("업로드 가능한 파일유형이 아닙니다.");
                }
            }
            catch (Exception exception)
            {
                WriteRichTextBoxText(exception.ToString());
            }
        }

        //내용 지우기 button
        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox.Text = "";
        }


        public static void WriteRichTextBoxText(string text)
        {
            richTextBox.Text += $"{text}\r\n";
        }
    }       
}

