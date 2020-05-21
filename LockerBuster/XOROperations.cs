using System.Text;

namespace LockerBuster
{
    class XOROperations
    {
        private static byte[] keyBytes = Encoding.UTF8.GetBytes("handyapps@gmail.com");

        public static byte[] Decrypt(byte[] input)
        {
            var output = new byte[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                output[i] = (byte)(input[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return output;
        }

        public static byte[] Encrypt(byte[] input)
        {
            var output = new byte[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                output[i] = (byte)(input[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return output;
        }
    }
}
