using System;
using System.Text;

using Org.BouncyCastle.Crypto;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;


namespace EdGo.Crypto
{
    class DESAlgorithm : CryptoAlgorithm
    {

        DesParameters key = null;
        BufferedBlockCipher ecipher = null;
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

            byte[] cipherbytes = ecipher.DoFinal(bytesToEncrypt);
            return Convert.ToBase64String(cipherbytes);
        }

        public string decode(String source)
        {
            throw new NotImplementedException();
        }

    }
}
