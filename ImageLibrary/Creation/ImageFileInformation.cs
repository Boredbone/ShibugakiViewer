using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Tools;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;

namespace ImageLibrary.Creation
{
    /// <summary>
    /// 画像ファイル情報を取得する範囲
    /// </summary>
    public enum PropertiesLevel
    {
        None = 0,
        Size = 1,
        Basic = 2,
        Shell = 3,
    }

    /// <summary>
    /// 画像ファイルのプロパティ
    /// </summary>
    public class ImageFileInformation
    {
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset DateModified { get; set; }

        public long Size { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public int Rating { get; set; }
        public HashSet<string> Keywords { get; set; }

        public string Name { get; set; }
        public string Path { get; set; }

        public bool IsNotFound { get; set; }
    }


    public static class ImageFileUtility
    {

        private static TimeSpan timeOffset = DateTimeOffset.Now.Offset;

        public static DateTimeOffset ConvertDateTime(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, timeOffset);
        }

        public static DateTimeOffset ConvertDateTime(DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Offset);
        }


        public static void UpdateInformation(Record record, bool size, bool date)
        {
            try
            {
                var info = new System.IO.FileInfo(record.FullPath);

                if (size)
                {
                    record.Size = info.Length;
                }
                if (date)
                {
                    record.DateCreated = ConvertDateTime(info.CreationTime);
                    record.DateModified = ConvertDateTime(info.LastWriteTime);
                }
            }
            catch
            {
                //No operation
            }
        }
    }
}
