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
            string outputFile = Environment.CurrentDirectory + "\\" + args[0] + ".splash";
            if (!File.Exists(logoFile)) {
                Console.WriteLine(args[0] + ": File not found!");
                return;
            }
            if (File.Exists(outputFile)) {
                File.Delete(outputFile);
            }
            try { 
                Bitmap logo = new Bitmap(logoFile);
                byte[] output = MakeLogoImage(logo);
                FileStream stream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write);
                stream.Write(output,0,output.Length);
                stream.Flush();
                stream.Close();
                stream.Dispose();
                Console.WriteLine(args[0] + ".splash: File saved successfully!");
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void ShowUsage() {
            Console.WriteLine("Usage: LogoGen.exe <logo.png>");
            Environment.Exit(0);
        }

        const int SECTOR_SIZE_IN_BYTES = 512; // Header size
        public struct SplashHeader
        {
            public int width;
            public int height;
            public bool compressed;
            public int real_bytes;
            public SplashHeader(int width, int height, bool compressed, int real_bytes)
            {
                this.width = width;
                this.height = height;
                this.compressed = compressed;
                this.real_bytes = real_bytes;
            }

            public override string ToString()
            {
                return "width: "+width + ", height: " + height + ", type: " + (compressed ? 1 : 0) + ", real_bytes: " + real_bytes;
            }
        }


        public static SplashHeader? GetImgHeader(byte[] splash)
        {
            string magic = "SPLASH!!";
            int width = 0;
            int height = 0;
            bool compressed = splash[16] == 1;
            int real_size = 0;
            for (int i = 0; i < SECTOR_SIZE_IN_BYTES; i++)
            {
                int b = bunpack(splash[i]);
                // 检验开头
                if(i < 8 && splash[i] != magic[i])
                {
                    return null;
                }

                // width
                if (i >= 8 && i <= 11)
                {
                    width += b << ((i - 8) * 8);
                }

                // height
                if (i >= 12 && i <= 15)
                {
                    height += b << ((i - 12) * 8);
                }

                // block number
                if (i >= 20 && i <= 23)
                {
                    real_size += b << ((i - 20) * 8);
                }
            }
            int real_bytes = (512 * real_size) - 511;
            return new SplashHeader(width, height, compressed, real_bytes);
        }

        public static byte[] GetImgHeader(Size size,
            bool compressed = false, int real_bytes = 0)
        {
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
        public static List<Result> encode(List<int> line)
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

        public static List<int> decode(List<Result> lst)
        {
            List<int> line = new List<int>();
            foreach(Result run in lst) {

            }
            throw new NotImplementedException();
            return line;
        }

        public static byte[] encodeRLE24(Bitmap img)
        {
            int width = img.Width;
            int height = img.Height;
            List<byte> output = new List<byte>();
            Console.WriteLine("width: " + width + ", height: " + height);
            for (int h = 0; h < height; h++)
            {
                List<int> line = new List<int>();
                for (int w = 0; w < width; w++)
                {
                    Color c = img.GetPixel(w, h);
                    line.Add((c.R << 16) + (c.G << 8) + c.B);
                }
                List<Result> result = encode(line);
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

        private static Bitmap decodeRLE24(SplashHeader header, byte[] output)
        {
            int width = header.width;
            int height = header.height;
            Bitmap img = new Bitmap(width, height);
            int h = -1;
            int temp_pixel = 0;
            int step = -1;
            int count = 0;
            List<Result> result = new List<Result>();
            List<int> pixels = new List<int>();
            for (int i = 0; i < output.Length; i++)
            {
                int b = bunpack(output[i]);
                // count == 256 是每一行的开头
                if (b == 256)
                {
                    if (result.Count > 0)
                    {
                        List<int> line = decode(result);
                        for (int w = 0; w < line.Count; w++)
                        {
                            int rgb = line[w];
                            img.SetPixel(w, h, Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF));
                        }
                        result.Clear();
                    }
                    h++;
                }
                if (step < 0)
                {
                    count = bunpack(output[i]) + 1;
                    step = 0;
                    continue;
                }

                temp_pixel += bunpack(output[i]) << ((step % 3) * 8);

                if (step % 3 == 2)
                {
                    if (count > 128)
                    {
                       
                        result.Add(new Result(count, new int[] { temp_pixel }));
                        temp_pixel = 0;
                        step = -1;
                        continue;
                    }
                    else
                    {
                        // 已确定，当 count <= 128 时，pixel.Length == count

                        // result的数量貌似是和width有关的，为了能够分行，应研究从何处分割行
                        // 每个 result 的头部必有 count == 256
                        pixels.Add(temp_pixel);
                        temp_pixel = 0;
                        if (step >= count)
                        {
                            result.Add(new Result(count, pixels));
                            pixels.Clear();
                            step = -1;
                            continue;
                        }
                    }
                }
                step++;
            }
            return img;
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
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    return stream.GetBuffer();
                }
            }
        }

        public static Bitmap GetBodyImage(SplashHeader header, byte[] img)
        {
            // REL 压缩
            if (header.compressed)
                return decodeRLE24(header, img);
            else
            {
                // 将 bgr 颠倒为 rgb
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Write(img, 0, img.Length);
                    Bitmap background = new Bitmap(stream);
                    Bitmap bitmap = new Bitmap(header.width, header.height);
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            Color c = background.GetPixel(x, y);
                            bitmap.SetPixel(x, y, Color.FromArgb(c.B, c.G, c.R));
                        }
                    }
                    return bitmap;
                }
            }
        }

        public static byte[] MakeLogoImage(Bitmap bitmap)
        {
            byte[] body = GetImageBody(bitmap, SUPPORT_RLE24_COMPRESSIONT);
            byte[] header = GetImgHeader(bitmap.Size, SUPPORT_RLE24_COMPRESSIONT, body.Length);
            return Connect(header, body);
        }

        public static Bitmap MakeLogoImage(byte[] singleSplash)
        {
            return new Bitmap(1,1);
        }

        public static byte[] Connect(params byte[][] bytes) 
        {
            List<byte> output = new List<byte>();
            foreach (byte[] entry in bytes)
            {
                foreach (byte b in entry) 
                {
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
        public static byte bpack(int i)
        {
            return Convert.ToByte(Convert.ToChar(i));
        }

        public static int bunpack(byte b)
        {
            return Convert.ToInt32(Convert.ToChar(b));
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