using Neo;
using NeoTestHarness;

namespace DevHawk.RegistrarTests
{
    public static class Common
    {
        public const string DOMAIN_NAME = "sample.domain";
        public readonly static byte[] DOMAIN_NAME_BYTES = Neo.Utility.StrictUTF8.GetBytes(DOMAIN_NAME);
        public readonly static UInt160 BOB = "NXZQqdnqQKFxQfgMVwmV59yU8tf1P28tEM".FromAddress();
        public readonly static UInt160 ALICE = "NhGxW6BtLRhFLqh2oWqeRpNj8aNzKybRoV".FromAddress();
    }
}

