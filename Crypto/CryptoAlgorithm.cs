using System;

namespace EdGo.Crypto
{
    interface CryptoAlgorithm
    {
        String encode(String source);
        String decode(String source);
    }
}
