using System.Security.Cryptography;
using System.Text;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class HashArquivoService : IHashArquivoService
    {
        public string ComputeSha256(byte[] content)
        {
            var hash = SHA256.HashData(content);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (var item in hash)
            {
                builder.Append(item.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
