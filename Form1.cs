﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Collections;
using System.Threading;
using Gif.Components;
using System.Drawing.Drawing2D;
namespace Image2Char
{
    public partial class Form1 : Form
    {

        protected Image image;
        protected Bitmap bitmap;

        protected int model = 1;
        protected Hashtable htGif;
        protected Hashtable htCharToBmp;

        protected List<Image> imageList;

        protected delegate void CharShowCallBack(string s);
        private CharShowCallBack charshowCallBack;

        public float Progress { get => float.Parse(label1.Text); }
        public string ImageName { set; get; }
        public bool isTextShow { get => cB_TextShow.Checked; }
        public bool isPicShow { get => cB_PicShow.Checked; }
        public bool isBroswerShow { get => cB_BrowserShow.Checked; }
        public bool isParallel { get => cB_Parallel.Checked; }
        public bool isOpenFloder { get => cB_OpenFloder.Checked; }
        public bool isFormShow { get => cB_FormShow.Checked; }
        /// <summary>
        /// 图片生成后所存放的路径
        /// </summary>
        public string ImagePath { set; get; }

        /// <summary>
        /// 是否压缩图像
        /// </summary>
        public bool ImageCompress { set; get; }

        /// <summary>
        /// 图片显示在浏览器中的宽度
        /// </summary>
        public decimal BrowserWidth { get => nud_Width.Value; }
        public decimal BrowserHeight { get => nud_Height.Value; }


        public float TextFontSize { get => tBar_CharSize.Value; }
        public static int DisplaySpeed { set; get; }

        public Form1()
        {
            InitializeComponent();
            tb_CharImage.Font = new Font(tb_CharImage.Font.Name, TextFontSize);
            DisplaySpeed = (tBar_Speed.Maximum - tBar_Speed.Value + 1) * 40;
            //charshowCallBack = new CharShowCallBack(ShowGifChar);
            Config.messageClass.OnMessageSend += new MessageEventHandler(SubthreadMessageReceive);
        }
        /// <summary>
        /// 消息处理
        /// </summary>
        /// <param name="e"></param>
        private void MessageManage(MessageEventArgs e)
        {
            switch (e.messageType)
            {
                case MessageType.RunTime:

                    break;
                case MessageType.Message:
                    tb_CharImage.Text = e.Message;
                    //tb_CharImage.Refresh();
                    break;
                case MessageType.Error:

                    break;
                case MessageType.Progress:
                    label1.Text = e.Progress.ToString();
                    if (e.Progress > 100)
                        pB_HandleProgress.Value = 100;
                    else
                        pB_HandleProgress.Value = (int)e.Progress;
                    break;
                case MessageType.ImageInfo:
                    pB_CharGif.Image = e.imageinfo;
                    //pB_CharGif.Refresh();
                    break;
            }
        }
        private void SubthreadMessageReceive(MessageEventArgs e)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    MessageEventHandler handler = new MessageEventHandler(MessageManage);
                    this.Invoke(handler, new object[] { e });
                }
            }
            catch (Exception)
            {
                //throw new Exception("", ex);
            }
        }
        /// <summary>
        /// 将图片转换为字符画
        /// </summary>
        /// <param name="bitmap">Bitmap类型的对象</param>
        /// <param name="savaPath">保存路径</param>
        /// <param name="WAddNum">宽度缩小倍数（如果输入3，则以1/3倍的宽度显示）</param>
        /// <param name="HAddNum">高度缩小倍数（如果输入3，则以1/3倍的高度显示）</param>
        public static StringBuilder ConvertToChar(Bitmap bitmap, int WAddNum, int HAddNum, String savaPath = "")
        {
            StringBuilder sb = new StringBuilder();
            String replaceChar = "@*#$%XB0H?OC7>+v=~^:_-'`. ";
            for (int i = 0; i < bitmap.Height; i += HAddNum)
            {
                for (int j = 0; j < bitmap.Width; j += WAddNum)
                {
                    //获取当前点的Color对象
                    Color c = bitmap.GetPixel(j, i);
                    //计算转化过灰度图之后的rgb值（套用已有的计算公式）
                    int rgb = (int)(c.R * .3 + c.G * .59 + c.B * .11);
                    //计算出replaceChar中要替换字符的index
                    //所以根据当前灰度所占总rgb的比例(rgb值最大为255，为了防止超出索引界限所以/256.0)
                    //（肯定是小于1的小数）乘以总共要替换字符的字符数，获取当前灰度程度在字符串中的复杂程度
                    int index = (int)(rgb / 256.0 * replaceChar.Length);
                    sb.Append(replaceChar[index]);
                }
                //添加换行
                sb.Append("\r\n");
            }
            return sb;
        }

        private void bt_SelectImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog oi = new OpenFileDialog
            {
                //oi.InitialDirectory = "c:\\";
                Filter = "图片(*.jpg) | *.jpg;*.gif;*.bmp| 所有文件(*.*) | *.*",
                RestoreDirectory = true,
                FilterIndex = 1
            };
            if (oi.ShowDialog() == DialogResult.OK)
            {
                var filename = oi.FileName;
                ImageName = Path.GetFileNameWithoutExtension(filename);
                if (Path.GetExtension(filename).ToLower() == ".gif")
                {
                    image = Image.FromFile(filename);
                    pictureBox1.Image = image;
                    GetFrames(filename);
                    model = 2;
                }
                else
                {
                    image = Image.FromFile(filename);
                    bitmap = new Bitmap(image);
                    pictureBox1.Image = image;
                    model = 1;
                }
            }
        }

        private void bt_Change_Click(object sender, EventArgs e)
        {
            ImageCompress = cB_Compress.Checked;
            Thread td = new Thread(HandleImage)
            {
                IsBackground = true
            };
            td.Start();
        }

        private void Write(StringBuilder sb, string savaPath)
        {
            //创建文件流
            using (FileStream fs = new FileStream(savaPath, FileMode.Create, FileAccess.Write))
            {
                //转码
                byte[] bs = Encoding.Default.GetBytes(sb.ToString());
                //写入
                fs.Write(bs, 0, bs.Length);
            }
        }

        private void tBar_CharSize_Scroll(object sender, EventArgs e)
        {
            tb_CharImage.Font = new Font(tb_CharImage.Font.Name, TextFontSize);
        }

        /// <summary>
        /// 获取图片中的各帧
        /// </summary>
        /// <param name="path">图片路径</param>
        /// <param name="pSavePath">保存路径</param>
        private void GetFrames(string path)
        {
            Image gif = Image.FromFile(path);
            FrameDimension fd = new FrameDimension(gif.FrameDimensionsList[0]);
            htGif = new Hashtable();
            imageList = new List<Image>();
            //获取帧数(gif图片可能包含多帧，其它格式图片一般仅一帧)
            int count = gif.GetFrameCount(fd);

            //以Jpeg格式保存各帧
            for (int i = 0; i < count; i++)
            {
                gif.SelectActiveFrame(fd, i);
                MemoryStream stream = new MemoryStream();
                gif.Save(stream, ImageFormat.Jpeg);
                stream.Position = 0;
                imageList.Add(Image.FromStream(stream));
                //gif.Save(Application.StartupPath + "\\frame_" + i + ".jpg", ImageFormat.Jpeg);
            }
        }

        private void GetGifChar(int w, int h)
        {
            int count = imageList.Count;
            float perProgress = ((100.0f - Progress) / count / 2);
            for (int i = 0; i < count; i++)
            {
                var bp = new Bitmap(imageList[i]);
                var sb = ConvertToChar(bp, w, h);
                htGif.Add(i, sb.ToString());
                ShowMessage(Progress + perProgress);
            }
            imageList.Clear();
        }

        private void ShowGifChar()
        {
            //List<Image> im = new List<Image>();
            //for (int i = 0; i < imageList.Count; i++)
            //{
            //    var bp = new Bitmap(imageList[i]);
            //    Image img = Image.FromHbitmap(bp.GetHbitmap());
            //    im.Add(img);
            //}
            while (true)
            {
                if (isFormShow )
                {
                    for (int i = 0; i < htCharToBmp.Count; i++)
                    {
                        if (isPicShow)
                            ShowMessage((Image)htCharToBmp[i]);
                        if (isTextShow)
                            ShowMessage((string)htGif[i], MessageType.Message);
                        Thread.Sleep(DisplaySpeed);
                    }
                }
                Thread.Sleep(1);
            }

        }

        private void HandleImage()
        {
            ShowMessage(1);
            int WAddNum = 1;
            int HAddNum = 1;
            switch (model)
            {
                case 1://图片
                    var sb = ConvertToChar(bitmap, WAddNum, HAddNum);
                    //Write(sb, Application.StartupPath+"\\1.txt");
                    ShowMessage(sb.ToString(), MessageType.Message);
                    break;
                case 2://gif动态图
                    GetGifChar(WAddNum, HAddNum);
                    TextToBitmap();
                    //ImageToGif();
                    GenerateHtml();
                    ShowMessage(100.00f);
                        ShowGifChar();
                    break;

            }
        }

        private void ShowMessage(Image img)
        {
            Config.messageClass.MessageSend(new MessageEventArgs(img));
        }

        private void ShowMessage(float pro, MessageType mt = MessageType.Progress)
        {
            Config.messageClass.MessageSend(new MessageEventArgs(pro, mt));
        }

        private void ShowMessage(string mes, MessageType mt)
        {
            Config.messageClass.MessageSend(new MessageEventArgs(mes, mt));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //jsGif();
            GenerateHtml();
            //MessageBox.Show(BrowserMode.GetBrowserVersion().ToString());
        }

        /// <summary>
        /// 把文字转换成Bitmap
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="rect">用于输出的矩形，文字在这个矩形内显示，为空时自动计算</param>
        /// <param name="fontcolor">字体颜色</param>
        /// <param name="backColor">背景颜色</param>
        /// <returns></returns>
        private Bitmap TextToBitmap(string text, Font font, Rectangle rect, Color fontcolor, Color backColor)
        {
            Graphics g;
            Bitmap bmp;
            StringFormat format = new StringFormat(StringFormatFlags.NoClip);
            if (rect == Rectangle.Empty)
            {
                bmp = new Bitmap(1, 1);
                g = Graphics.FromImage(bmp);
                //计算绘制文字所需的区域大小（根据宽度计算长度），重新创建矩形区域绘图
                SizeF sizef = g.MeasureString(text, font, PointF.Empty, format);

                int width = (int)(sizef.Width + 1);
                int height = (int)(sizef.Height + 1);
                rect = new Rectangle(0, 0, width, height);
                bmp.Dispose();
                Thread.Sleep(1);
                bmp = new Bitmap(width, height);
            }
            else
            {
                bmp = new Bitmap(rect.Width, rect.Height);
            }

            g = Graphics.FromImage(bmp);

            //使用ClearType字体功能
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.FillRectangle(new SolidBrush(backColor), rect);
            g.DrawString(text, font, Brushes.Black, rect, format);
            if(ImageCompress)
                bmp = KiResizeImage(bmp, 10);
            return bmp;
        }

        /// <summary>  
        /// Resize图片  
        /// </summary>  
        /// <param name="bmp">原始Bitmap</param>  
        /// <param name="percent">压缩率 如10为压缩至10%，即压缩90%</param>  
        /// <returns>处理以后的Bitmap</returns>  
        private Bitmap KiResizeImage(Bitmap bmp, float percent)
        {
            try
            {
                var oldW = bmp.Width;
                var oldH = bmp.Height;
                int newW = (int)(oldW * (percent / 100));
                int newH = (int)(oldH * (percent / 100));
                Bitmap b = new Bitmap(newW, newH);
                Graphics g = Graphics.FromImage(b);

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.DrawImage(bmp, new Rectangle(0, 0, newW, newH), new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                g.Dispose();

                return b;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 字符画string转图片，防闪烁，但内存占用极高
        /// </summary>
        private void TextToBitmap()
        {
            int count = htGif.Count;
            float perProgress = ((100.0f - Progress) / count);
            htCharToBmp = new Hashtable();
            var path = Application.StartupPath + "\\" + ImageName + "\\" + "resource" + "\\";
            ImagePath = path;
            if (!FloderExist(path))
            {

            }
            for (int i = 0; i < count; i++)
            {
                var val = TextToBitmap((string)htGif[i], tb_CharImage.Font, Rectangle.Empty, tb_CharImage.ForeColor, tb_CharImage.BackColor);
                path = ImagePath;
                path += i + ".jpg";
                val.Save(path);
                htCharToBmp.Add(i, val);
                ShowMessage(Progress + perProgress);
            }
        }

   


        private bool FloderExist(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 图片转GIF，速度极慢，可能是图片太大或者库本身的原因
        /// </summary>
        private void ImageToGif()
        {
            AnimatedGifEncoder e = new AnimatedGifEncoder();
            var path = Application.StartupPath + "\\" + ImageName + ".gif";
            FileStream fileStream = new FileStream(path, FileMode.Create);
            //MemoryStream stream = new MemoryStream();
            e.Start(fileStream);
            e.SetDelay(20);
            e.SetRepeat(0);
            for (int i = 0; i < htCharToBmp.Count; i++)
                e.AddFrame((Image)htCharToBmp[i]);//imageList[i]);
            e.Finish();
            var image = Image.FromFile(path);
            ShowMessage(image);
        }


        private void jsGif()
        {
            string path = ImagePath + @"\index.html";
            webBrowser1.Navigate(new System.Uri(path, UriKind.Absolute));
            //webKitBrowser1.Navigate("https://www.baidu.com");
            System.Diagnostics.Process.Start(path);
        }

        /// <summary>
        /// 根据模板生成HTML
        /// </summary>
        private void GenerateHtml()
        {
            HtmlClass htmlClass = new HtmlClass
            {
                template = Application.StartupPath + @"\template.html",
                path = ImagePath,
                htmlname = "index.html"
            };
            Dictionary<string, string> dic = new Dictionary<string, string>();
            {
                dic.Add("width", "800");
                dic.Add("height", "800");
                dic.Add("imageCount", htCharToBmp.Count.ToString());
                dic.Add("intervaltime", DisplaySpeed.ToString());

            }
            htmlClass.dic = dic;
            string error = "";
            string htmlpath = "";
            htmlClass.Create(ref error, ref htmlpath);
            if (isBroswerShow)
                System.Diagnostics.Process.Start(htmlpath);

        }

        private void pB_HandleProgress_Click(object sender, EventArgs e)
        {

        }

        private void tBar_Speed_Scroll(object sender, EventArgs e)
        {
            DisplaySpeed = (tBar_Speed.Maximum - tBar_Speed.Value + 1) * 40;
        }
    }
}