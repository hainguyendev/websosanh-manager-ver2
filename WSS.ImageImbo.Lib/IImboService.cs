using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSS.ImageImbo.Lib
{
    public interface IImboService
    {
        string PostImgToImbo(string url, string publicKey, string privateKey, string userName, string host, int port);
        string PostImgWithChangeTransference(string url, string publicKey, string privateKey, string userName, string host, int port);
    }
}
