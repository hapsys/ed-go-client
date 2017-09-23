using System;
using System.Text;
using Org.BouncyCastle.Crypto.Encodings;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;



namespace EdGo.Crypto
{
    class RSAAlgorithm : CryptoAlgorithm
    {

        private RsaKeyParameters key = null;
        private Pkcs1Encoding ecipher = null;
        private Pkcs1Encoding dcipher = null;
        //private RsaEngine dcipher = null;


        public RSAAlgorithm(RsaKeyParameters key)
        {
            this.key = key;
        }

        public string encode(string source)
        {
            if (ecipher == null)
            {
                ecipher = new Pkcs1Encoding(new RsaEngine());
                ecipher.Init(true, key);
            }

            byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(source);
            int inLength = ecipher.GetInputBlockSize();

            int resultLength = bytesToEncrypt.Length / inLength + (bytesToEncrypt.Length % inLength > 0 ? 1 : 0);
            byte[] cipherbytes = new byte[resultLength * ecipher.GetOutputBlockSize()];
            int start = 0;
            int copyIdx = 0;
            int length = (bytesToEncrypt.Length - start) > inLength ? inLength : bytesToEncrypt.Length - start;
            while (start < bytesToEncrypt.Length)
            {
                byte[] encBuffer = new byte[length];
                byte[] buffer = ecipher.ProcessBlock(bytesToEncrypt, start, length);
                Array.Copy(buffer, 0, cipherbytes, copyIdx, buffer.Length);
                start += inLength;
                copyIdx += buffer.Length;
                length = (bytesToEncrypt.Length - start) > inLength ? inLength : bytesToEncrypt.Length - start;
            }

            return Convert.ToBase64String(cipherbytes);

        }

        public string decode(string source)
        {
            if (dcipher == null)
            {
                dcipher = new Pkcs1Encoding(new RsaEngine());
                //dcipher = new RsaEngine();
                dcipher.Init(false, key);
            }

            byte[] bytesToDecrypt = Convert.FromBase64String(source);
            int inLength = dcipher.GetInputBlockSize();
            int outLength = dcipher.GetOutputBlockSize();
            Console.WriteLine(outLength);

            int resultLength = bytesToDecrypt.Length / inLength + (bytesToDecrypt.Length % inLength > 0 ? 1 : 0);
            //byte[] cipherbytes = new byte[resultLength * outLength];
            byte[] cipherbytes = new byte[bytesToDecrypt.Length];
            int start = 0;
            int copyIdx = 0;
            int length = (bytesToDecrypt.Length - start) > inLength ? inLength : bytesToDecrypt.Length - start;
            while (start < bytesToDecrypt.Length)
            {
                byte[] encBuffer = new byte[length];
                byte[] buffer = dcipher.ProcessBlock(bytesToDecrypt, start, length);
                Array.Copy(buffer, 0, cipherbytes, copyIdx, buffer.Length);
                start += inLength;
                copyIdx += buffer.Length;
                length = (bytesToDecrypt.Length - start) > inLength ? inLength : bytesToDecrypt.Length - start;
            }

            char[] charsToTrim = {'\0'};
            String result = System.Text.Encoding.UTF8.GetString(cipherbytes).TrimEnd(charsToTrim);

            return result;
        }


    }
}
