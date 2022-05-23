using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace LogoGen
{
    class Program
    {
        public const bool SUPPORT_RLE24_COMPRESSIONT = true;
        static void Main(string[] args)
        {
            if (args.Length != 1) {
                ShowUsage();
            }
            string logoFile = Environment.CurrentDirectory + "\\" + args[0];
            string outputFile = Environment.CurrentDirectory + "\\" + args[0];
            if (!File.Exists(logoFile)) {
                Console.WriteLine(args[0] + ": File not found!");
                return;
            }
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
            Bitmap logo = new Bitmap(logoFile);
            byte[] output = MakeLogoImage(logo);
            FileStream stream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write);
            stream.Write(output,0,output.Length);
            stream.Flush();
            stream.Close();
            stream.Dispose();
            Console.WriteLine(args[0] + ".splash: File saved successfully!");
        }

        public static void ShowUsage() {
            Console.WriteLine("Usage: LogoGen.exe <logo.png>");
            Environment.Exit(0);
        }

        public static byte[] GetImgHeader(Size size,
            bool compressed = false, int real_bytes = 0)
        {
            const int SECTOR_SIZE_IN_BYTES = 512; // Header size
            int[] header = new int[SECTOR_SIZE_IN_BYTES];
            Array.Fill(header, 0);

            int width = size.Width;
            int height = size.Height;
            int real_size = (real_bytes + 511) / 512;

            // magic
            string magic = "SPLASH!!";
            for (int i = 0; i < 8; i++)
            {
                header[i] = magic[i];
            }

            // width
            header[8] = (width & 0xFF);
            header[9] = ((width >> 8) & 0xFF);
            header[10] = ((width >> 16) & 0xFF);
            header[11] = ((width >> 24) & 0xFF);

            // height
            header[12] = (height & 0xFF);
            header[13] = ((height >> 8) & 0xFF);
            header[14] = ((height >> 16) & 0xFF);
            header[15] = ((height >> 24) & 0xFF);

            // type
            header[16] = ((compressed ? 1 : 0) & 0xFF);
            // header[17] = 0;
            // header[18] = 0;
            // header[19] = 0;

            // block number
            header[20] = (real_size & 0xFF);
            header[21] = ((real_size >> 8) & 0xFF);
            header[22] = ((real_size >> 16) & 0xFF);
            header[23] = ((real_size >> 24) & 0xFF);

            byte[] output = new byte[header.Length];
            for (int i = 0; i < header.Length; i++)
            {
                output[i] = bpack(header[i]);
            }
            return output;
        }
        private static List<Result> encode(List<int> line)
        {
            int count = 0;
            List<Result> lst = new List<Result>();
            int repeat = -1;
            List<int> run = new List<int>();
            int total = line.Count - 1;
            for (int index = 0; index < line.Count - 1; index++) {
                int current = line[index];
                if (current != line[index + 1]) {
                    run.Add(current);
                    count += 1;
                    if (repeat == 1) {
                        lst.Add(new Result(count + 128, run));
                        count = 0;
                        run.Clear();
                        repeat = -1;
                        if (index == total - 1)
                        {
                            run.Add(line[index + 1]);
                            lst.Add(new Result(1, run));
                        }
                    }
                    else {
                        repeat = 0;

                        if (count == 128) {
                            lst.Add(new Result(128, run));
                            count = 0;
                            run.Clear();
                            repeat = -1;
                        }
                        if (index == total - 1) {
                            run.Add(line[index + 1]);
                            lst.Add(new Result(count + 1, run));
                        }
                    }
                }
                else
                {
                    if (repeat == 0) {
                        lst.Add(new Result(count, run));
                        count = 0;
                        run.Clear();
                        repeat = -1;
                        if (index == total - 1) {
                            run.Add(line[index + 1]);
                            run.Add(line[index + 1]);
                            lst.Add(new Result(2 + 128, run));
                            break;
                        }
                    }
                    run.Add(current);
                    repeat = 1;
                    count += 1;
                    if (count == 128) {
                        lst.Add(new Result(256, run));
                        count = 0;
                        run.Clear();
                        repeat = -1;
                    }
                    if (index == total - 1) {
                        if (count == 0) {
                            run.Clear();
                            run.Add(line[index + 1]);
                            lst.Add(new Result(1, run));
                        }
                        else
                        {
                            run.Add(current);
                            lst.Add(new Result(count + 1 + 128, run));
                        }
                    }
                }
            }
            return lst;
        }

        private static byte[] encodeRLE24(Bitmap img)
        {
            int width = img.Width;
            int height = img.Height;
            List<byte> output = new List<byte>();

            for (int h = 0; h < height; h++)
            {
                List<int> line = new List<int>();
                List<Result> result = new List<Result>();
                for (int w = 0; w < width; w++)
                {
                    Color c = img.GetPixel(w, h);
                    line.Add((c.R << 16) + (c.G << 8) + c.B);
                }
                result = encode(line);
                foreach (Result r in result)
                {
                    int count = r.count;
                    int[] pixel = r.pixel;
                    output.Add(bpack(count - 1));
                    if (count > 128)
                    {
                        output.Add(bpack((pixel[0]) & 0xFF));
                        output.Add(bpack(((pixel[0]) >> 8) & 0xFF));
                        output.Add(bpack(((pixel[0]) >> 16) & 0xFF));
                    }
                    else
                    {
                        foreach (int item in pixel)
                        {
                            output.Add(bpack((item) & 0xFF));
                            output.Add(bpack((item >> 8) & 0xFF));
                            output.Add(bpack((item >> 16) & 0xFF));
                        }
                    }
                }
            }
            return output.ToArray();
        }

        public static byte[] GetImageBody(Bitmap img, bool compressed = false)
        {
            Bitmap background = new Bitmap(img.Width, img.Height);
            Graphics g = Graphics.FromImage(background);
            g.Clear(Color.FromArgb(0, 0, 0));
            g.DrawImage(img, 0, 0);
            // REL 压缩
            if (compressed)
                return encodeRLE24(background);
            else
            {
                // 将 rgb 颠倒为 bgr
                Bitmap bitmap = new Bitmap(background.Width, background.Height);
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color c = background.GetPixel(x, y);
                        bitmap.SetPixel(x, y, Color.FromArgb(c.B, c.G, c.R));
                    }

                }
                MemoryStream stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] data = stream.GetBuffer();
                stream.Close();
                return data;
            }
        }

        public static byte[] MakeLogoImage(Bitmap bitmap)
        {
            byte[] body = GetImageBody(bitmap, SUPPORT_RLE24_COMPRESSIONT);
            byte[] header = GetImgHeader(bitmap.Size, SUPPORT_RLE24_COMPRESSIONT, body.Length);
            return Connect(header, body);
        }
        public static byte[] Connect(params byte[][] bytes) {
            List<byte> output = new List<byte>();
            foreach (byte[] entry in bytes) {
                foreach (byte b in entry) {
                    output.Add(b);
                }
            }
            return output.ToArray();
        }
        /// <summary>
        /// 暂不确定的python方法
        /// struct.pack("B", i)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static byte bpack(int i) {
            return Convert.ToByte(Convert.ToChar(i));
        }
        public struct Result
        {
            public int count;
            public int[] pixel;
            public Result(int count, int[] pixel)
            {
                this.count = count;
                this.pixel = pixel;
            }
            public Result(int count, List<int> pixel)
            {
                this.count = count;
                this.pixel = pixel.ToArray();
            }
        }
    }
}