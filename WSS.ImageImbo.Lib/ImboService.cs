using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace WSS.ImageImbo.Lib
{
    public class ImboService
    {
        private static string ToHexString(byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private static string CreateToken(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = new ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return ToHexString(hashmessage);
            }
        }
        private static bool ContainsTransparent(Bitmap image)
        {
            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    if (image.GetPixel(x, y).A != 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string PostImgToImboChangeBackgroundTransference(string url, string publicKey, string privateKey, string userName, string host, int port)
        {
            if (!url.ToLower().Contains(".png"))
                return PostImageToImbo(url, publicKey, privateKey, userName, host, port);


            string dir = Path.GetTempPath();
            string pathTemp = dir + "/" + Guid.NewGuid().ToString() + ".png";
            string idImageNew = "";
            //download image
            url = url.Replace(@"///", @"//").Replace(@"////", @"//");
            var regexhttp = Regex.Match(url, "http").Captures;
            if (regexhttp.Count > 1)
                url = url.Substring(url.LastIndexOf("http", StringComparison.Ordinal));
            else if (regexhttp.Count == 0)
                url = "http://" + url;
            var requestdownload = (HttpWebRequest)WebRequest.Create(url);
            requestdownload.Credentials = CredentialCache.DefaultCredentials;
            requestdownload.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.124 Safari/537.36";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                   | SecurityProtocolType.Tls11
                                                   | SecurityProtocolType.Tls12
                                                   | SecurityProtocolType.Ssl3;
            ServicePointManager
                .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            var responseImageDownload = (HttpWebResponse)requestdownload.GetResponse();
            var streamImageDownload = responseImageDownload.GetResponseStream();
            Image myImage = System.Drawing.Image.FromStream(streamImageDownload);

          
            using (var b = new Bitmap(myImage.Width, myImage.Height))
            {
                b.SetResolution(myImage.HorizontalResolution, myImage.VerticalResolution);

                using (var g = Graphics.FromImage(b))
                {
                    g.Clear(Color.White);
                    g.DrawImageUnscaled(myImage, 0, 0);
                }
                b.Save(pathTemp, ImageFormat.Png);

            }
           
            // Imbo
            string urlQuery = host + ":" + port + @"/users/" + userName + @"/images";
            string strDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            string str = "POST" + "|" + host + ":" + port + @"/users/" + userName + @"/images" + "|" + publicKey + "|" + strDate;
            var signleData = CreateToken(str, privateKey);

            var request = (HttpWebRequest)WebRequest.Create(urlQuery);
            request.Headers.Add("X-Imbo-PublicKey", publicKey);
            request.Headers.Add("X-Imbo-Authenticate-Timestamp", strDate);
            request.Headers.Add("X-Imbo-Authenticate-Signature", signleData);
            request.ContentType = "application/json";
            request.Method = "POST";

            using (var streamPushToImbo = request.GetRequestStream())
            {
                var memoryStream = File.OpenRead(pathTemp);
                if (memoryStream != null) memoryStream.CopyTo(streamPushToImbo);
                memoryStream.Close();
            }

            using (WebResponse response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (StreamReader sr99 = new StreamReader(stream))
                    {
                        var responseContent = sr99.ReadToEnd();
                        dynamic d = JObject.Parse(responseContent);
                        idImageNew = d.imageIdentifier;
                    }
                }
            }

            File.Delete(pathTemp);
            return idImageNew;
        }

      

        public static string PostImageToImbo(string url, string publicKey, string privateKey, string userName, string host, int port)
        {
            string idImageNew = "";
            //download image
            url = url.Replace(@"///", @"//").Replace(@"////", @"//");
            var regexhttp = Regex.Match(url, "http").Captures;
            if (regexhttp.Count > 1)
                url = url.Substring(url.LastIndexOf("http", StringComparison.Ordinal));
            else if (regexhttp.Count == 0)
                url = "http://" + url;
            var requestdownload = (HttpWebRequest)WebRequest.Create(url);
            requestdownload.Credentials = CredentialCache.DefaultCredentials;
            requestdownload.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.124 Safari/537.36";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                   | SecurityProtocolType.Tls11
                                                   | SecurityProtocolType.Tls12
                                                   | SecurityProtocolType.Ssl3;
            ServicePointManager
                .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            var responseImageDownload = (HttpWebResponse)requestdownload.GetResponse();
            var streamImageDownload = responseImageDownload.GetResponseStream();

            // Imbo
            string urlQuery = host + ":" + port + @"/users/" + userName + @"/images";
            string strDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            string str = "POST" + "|" + host + ":" + port + @"/users/" + userName + @"/images" + "|" + publicKey + "|" + strDate;
            var signleData = CreateToken(str, privateKey);

            var request = (HttpWebRequest)WebRequest.Create(urlQuery);
            request.Headers.Add("X-Imbo-PublicKey", publicKey);
            request.Headers.Add("X-Imbo-Authenticate-Timestamp", strDate);
            request.Headers.Add("X-Imbo-Authenticate-Signature", signleData);
            request.ContentType = "application/json";
            request.Method = "POST";

            using (var streamPushToImbo = request.GetRequestStream())
            {
                if (streamImageDownload != null) streamImageDownload.CopyTo(streamPushToImbo);
            }

            using (WebResponse response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (StreamReader sr99 = new StreamReader(stream))
                    {
                        var responseContent = sr99.ReadToEnd();
                        dynamic d = JObject.Parse(responseContent);
                        idImageNew = d.imageIdentifier;
                    }
                }
            }
            return idImageNew;
        }
    }
}
