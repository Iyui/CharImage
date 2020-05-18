using System;
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
using LitJson;
namespace Image2Char
{
    public partial class Form1 : Form
    {

        protected Image image;
        protected Bitmap bitmap;

        protected int model = 1;
        protected Hashtable htGif;
        //protected Hashtable htCharToBmp;

        protected List<Image> imageList;

        Thread td;

        //protected delegate void CharShowCallBack(string s);
        //private CharShowCallBack charshowCallBack;

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

        public string HtmlPath { set; get; }

        /// <summary>
        /// 是否压缩图像
        /// </summary>
        public bool ImageCompress { set; get; }

        public static bool isContinue { set; get; }
        public int CompressRate { set; get; }
        /// <summary>
        /// 图片显示在浏览器中的宽度
        /// </summary>
        public decimal BrowserWidth { get => nud_Width.Value; }
        public decimal BrowserHeight { get => nud_Height.Value; }


        public float TextFontSize { get => tBar_CharSize.Value; }
        public static int DisplaySpeed { set; get; }
        public static bool isGenerateGif { set; get; }

        public Form1()
        {
            InitializeComponent();
            tb_CharImage.Font = new Font(tb_CharImage.Font.Name, TextFontSize);
            CompressRate = 100 - tb_CompressRate.Value;

            DisplaySpeed = (tBar_Speed.Maximum - tBar_Speed.Value + 1) * 50;
            isGenerateGif = cb_GeneGif.Checked;
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
                    if (e.Progress >= 100)
                    {
                        pB_HandleProgress.Value = 100;
                        btn_SelectImage.Enabled = true;
                        label1.Text = 100.ToString("0.000");
                    }
                    else
                    {
                        pB_HandleProgress.Value = (int)e.Progress;
                        label1.Text = e.Progress.ToString("0.000");
                    }
                    break;
                case MessageType.ImageInfo:
                    pB_CharGif.Image = e.imageinfo;
                    //pB_CharGif.Refresh();
                    break;
                case MessageType.PrgressInfo:
                    lInfo.Text = e.PrgressInfo;
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

        private void bt_Change_Click(object sender, EventArgs e)
        {
            if (!(ThreadImageToGif is null) && ThreadImageToGif.IsAlive)
                ThreadImageToGif.Abort();
            if (!(ThreadImageToShow is null) && ThreadImageToShow.IsAlive)
            {
                ThreadImageToShow.Abort();
            }
            if (image is null)
                bt_SelectImage_Click(sender, e);
            if (image is null)
            {
                MessageBox.Show("请选择一张图片进行转换");
                return;
            }
            ImageCompress = cB_Compress.Checked;
            btn_SelectImage.Enabled = false;
            if (!(td is null) && td.IsAlive)
                isContinue = false;
            td = new Thread(HandleImage)
            {
                IsBackground = true
            };
            td.Start();
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
                Filter = "图片(*.jpg,*.jpeg,*.gif,*.bmp) | *.jpg;*.jpeg;*.gif;*.bmp| 所有文件(*.*) | *.*",
                RestoreDirectory = true,
                FilterIndex = 1
            };
            if (oi.ShowDialog() == DialogResult.OK)
            {
                while (!(td is null) && td.IsAlive)
                {
                    ShowInfo("初始化");
                    isContinue = false;
                    Thread.Sleep(1);
                }
                var filename = oi.FileName;
                ImageName = Path.GetFileNameWithoutExtension(filename);
                var Format = new string[] {".gif",".jpg",".bmp" };
                if (Format.Contains(Path.GetExtension(filename).ToLower()))
                {
                    try
                    {
                        image = Image.FromFile(filename);
                    }
                    catch
                    {
                        MessageBox.Show("不正确的格式","错误的预期",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        return;
                    }
                    pictureBox1.Image = image;

                    GetFrames(filename);
                    model = 2;
                }
                
            }
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
            ShowInfo("正在解析GIF");
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
            isContinue = true;
           
            while (isContinue)
            {
                if (isFormShow)
                {
                    for (int i = 0; i < htGif.Count; i++)
                    {
                        if (!isContinue)
                            break;
                        if (isPicShow)
                        {
                            FileStream fileStream = new FileStream(ImagePath + i + ".jpg", FileMode.Open, FileAccess.Read);

                            int byteLength = (int)fileStream.Length;
                            byte[] fileBytes = new byte[byteLength];
                            fileStream.Read(fileBytes, 0, byteLength);

                            //文件流关閉,文件解除锁定
                            fileStream.Close();

                            Image image = Image.FromStream(new MemoryStream(fileBytes));

                            ShowMessage(image);
                            GC.Collect();
                        }
                        if (isTextShow)
                            ShowMessage((string)htGif[i], MessageType.Message);
                        Thread.Sleep(DisplaySpeed);
                    }
                }
                Thread.Sleep(1);
            }
            ShowInfo("");
        }
        Thread ThreadImageToShow;
        private void ImageToShow()
        {
            ThreadImageToShow = new Thread(ShowGifChar)
            {
                IsBackground = true
            };
            ThreadImageToShow.Start();
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
                    ShowMessage(100.00f);
                    break;
                case 2://gif动态图
                    GetGifChar(WAddNum, HAddNum);
                    TextToBitmap();
                    GenerateHtml();
                    GenerateHtmlChar();
                    OpenFloder();
                    tdImageToGif();
                    ImageToShow();
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

        private void ShowInfo(string mes)
        {
            Config.messageClass.MessageSend(new MessageEventArgs(mes,MessageType.PrgressInfo));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //jsGif();
            //GenerateHtml();
            //GenerateHtmlChar();
            //MessageBox.Show(BrowserMode.GetBrowserVersion().ToString());
            System.Diagnostics.Process.Start("explorer.exe", Application.StartupPath + "\\" + "resource" + "\\");
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
                bmp = KiResizeImage(bmp,CompressRate);
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
            ShowInfo("正在进行字符化处理");
            int count = htGif.Count;
            float perProgress = ((100.0f - Progress) / count);
            //htCharToBmp = new Hashtable();
            var path = Application.StartupPath + "\\" + "resource" + "\\" + ImageName + "\\"+"image"+"\\";
            ImagePath = path;
            HtmlPath = Application.StartupPath + "\\" + "resource" + "\\" + ImageName;

            if (!FloderExist(path))
            {

            }
            for (int i = 0; i < count; i++)
            {
                var val = TextToBitmap((string)htGif[i], new Font(tb_CharImage.Font.Name, 12), Rectangle.Empty, tb_CharImage.ForeColor, tb_CharImage.BackColor);
                if (val == null)
                    continue;
                path = ImagePath;
                path += i + ".jpg";
                
                val.Save(path);
                //htCharToBmp.Add(i, val);内存占用大，取消使用
                val.Dispose();
                GC.Collect();
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
            if (!isGenerateGif)
                return;
            AnimatedGifEncoder e = new AnimatedGifEncoder();
            var path = HtmlPath + "\\" + ImageName + ".gif";
            FileStream fileStream = new FileStream(path, FileMode.Create);
            //MemoryStream stream = new MemoryStream();
            e.Start(fileStream);
            e.SetDelay(DisplaySpeed);
            e.SetRepeat(0);
            for (int i = 0; i < htGif.Count; i++)
            {
                if (htGif.Count == 1)
                    break;
                Image image = Image.FromFile(ImagePath + i + ".jpg");
                ShowInfo($"正在生成GIF，剩余:{htGif.Count - i}帧");
                e.AddFrame(image);//imageList[i]);
                GC.Collect();
            }
            e.Finish();
            ShowInfo("处理已完成");
            ShowMessage(100.00f);
            //var image = Image.FromFile(path);
            //ShowMessage(image);
        }
        Thread ThreadImageToGif;
        private void tdImageToGif()
        {
            ThreadImageToGif = new Thread(ImageToGif)
            {
                IsBackground = true
            };
            ThreadImageToGif.Start();
        }



        /// <summary>
        /// 根据模板生成HTML
        /// </summary>
        private void GenerateHtml()
        {
            ShowInfo("正在生成HTML");
            HtmlClass htmlClass = new HtmlClass
            {
                template = Application.StartupPath + @"\template.html",
                path = HtmlPath,
                htmlname = "index.html"
            };
            Dictionary<string, string> dic = new Dictionary<string, string>();
            {
                dic.Add("width", "800");
                dic.Add("height", "800");
                dic.Add("imageCount", htGif.Count.ToString());
                dic.Add("intervaltime", DisplaySpeed.ToString());
            }
            htmlClass.dic = dic;
            string error = "";
            string htmlpath = "";
            htmlClass.Create(ref error, ref htmlpath);
            if (isBroswerShow)
                System.Diagnostics.Process.Start(htmlpath);

        }

        /// <summary>
        /// 根据模板生成HTML
        /// </summary>
        private void GenerateHtmlChar()
        {
            HtmlClass htmlClass = new HtmlClass
            {
                template = Application.StartupPath + @"\templateC.html",
                path = HtmlPath,
                htmlname = "indexC.html"
            };
            int strCount = htGif.Count;

            string json = JsonData();
            Dictionary<string, string> dic = new Dictionary<string, string>();
            {
                dic.Add("array", json);
                dic.Add("intervaltime", DisplaySpeed.ToString());
                dic.Add("arrCount", strCount.ToString());
            }
            htmlClass.dic = dic;
            string error = "";
            string htmlpath = "";
            htmlClass.Create(ref error, ref htmlpath);
            if (isBroswerShow)
                System.Diagnostics.Process.Start(htmlpath);
        }

        private string JsonData()
        {
            JsonData data = new JsonData();
         
            for (int i = 0; i < htGif.Count; i++)
            {
                var rep = ((string)htGif[i]).Replace(" ", "&nbsp");
                rep = rep.Replace("\r\n", "<br>");
                data[i.ToString()] = rep;
            }
            string content = data.ToJson();
            return content;
        }


        private void pB_HandleProgress_Click(object sender, EventArgs e)
        {

        }

        private void tBar_Speed_Scroll(object sender, EventArgs e)
        {
            DisplaySpeed = (tBar_Speed.Maximum - tBar_Speed.Value + 1) * 50;
        }

        private void OpenFloder()
        {
            if(isOpenFloder)
                System.Diagnostics.Process.Start("explorer.exe", ImagePath);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            JsonData data = new JsonData();


            data["1"] = "1 2 3 44444      5\r\n5";
            HtmlClass htmlClass = new HtmlClass
            {
                template = Application.StartupPath + @"\templateT.html",
                path = Application.StartupPath,
                htmlname = "indexT.html"
            };
      

            string json = data.ToJson();
            Dictionary<string, string> dic = new Dictionary<string, string>();
            {
                dic.Add("array", json);
                dic.Add("intervaltime", "1 2 3 44444      5\r\n5");
            }
            htmlClass.dic = dic;
            string error = "";
            string htmlpath = "";
            htmlClass.Create(ref error, ref htmlpath);
        }

        private void tb_CompressRate_Scroll(object sender, EventArgs e)
        {
            CompressRate = 100 - tb_CompressRate.Value;
            label6.Text = tb_CompressRate.Value+"%";
        }

        private void cB_Compress_CheckedChanged(object sender, EventArgs e)
        {
            tb_CompressRate.Enabled = cB_Compress.Checked;
            if (!cB_Compress.Checked)
            {
                if (DialogResult.No == MessageBox.Show("不进行图像压缩会导致处理过程生成GIF耗时较长,是否继续", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
                    cB_Compress.Checked = true;
            }

        }

        private void cb_GeneGif_CheckedChanged(object sender, EventArgs e)
        {
            isGenerateGif = cb_GeneGif.Checked;
        }


    }
}
