using System;
using System.Text;

using Org.BouncyCastle.Crypto;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;


namespace EdGo.Crypto
{
    class DESAlgorithm : CryptoAlgorithm
    {

        private DesParameters key = null;
        private BufferedBlockCipher ecipher = null;
        //DesEngine dcipher = null;


        public DESAlgorithm(String base64Key)
        {
            this.key = new DesParameters(Convert.FromBase64String(base64Key));
        }

        public string encode(String source)
        {
            if (ecipher == null)
            {
                DesEngine cipher = new DesEngine();
                cipher.Init(true, key);
                ecipher = new BufferedBlockCipher(cipher);
            }

            byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(source);

            int blockSize = ecipher.GetBlockSize();
            int resultLength = bytesToEncrypt.Length / blockSize + (bytesToEncrypt.Length % blockSize > 0 ? 1 : 0);

            byte[] buffer = new byte[resultLength * blockSize];

            Array.Copy(bytesToEncrypt, 0, buffer, 0, bytesToEncrypt.Length);

            byte[] cipherbytes = ecipher.DoFinal(buffer);

            return Convert.ToBase64String(cipherbytes);
        }

        public string decode(String source)
        {
            throw new NotImplementedException();
        }

    }
}
