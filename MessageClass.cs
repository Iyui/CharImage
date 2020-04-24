using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
namespace Image2Char
{

    public enum MessageType
    {
        Message,
        RunTime,
        FolderPath,
        FilePath,
        Error,
        Progress,
        ImageInfo,
    }

    public class MessageEventArgs : EventArgs
    {
        public MessageType messageType;
        public object oMessage;
        public String Message; //传递字符串信息
        public float Progress;
        public Image imageinfo;
        public MessageEventArgs(object obj, MessageType type)
        {
            this.oMessage = obj;
            this.messageType = type;
        }

        public MessageEventArgs(string message, MessageType type = MessageType.Message)
        {
            this.Message = message;
            this.messageType = type;
        }

        public MessageEventArgs(float progress, MessageType type = MessageType.Progress)
        {
            this.Progress = progress;
            this.messageType = type;
        }

        public MessageEventArgs(Image img, MessageType type = MessageType.ImageInfo)
        {
            this.imageinfo = img;
            this.messageType = type;
        }

    }

    public delegate void MessageEventHandler(MessageEventArgs e);

    public class MessageClass
    {
        public event MessageEventHandler OnMessageSend = null;

        public void MessageSend(MessageEventArgs e)
        {
            OnMessageSend?.Invoke(e);
        }
    }

    public static class Config
    {
        public static MessageClass messageClass = new MessageClass();
    }
}
