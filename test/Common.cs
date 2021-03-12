using System;
using System.IO.Abstractions;

namespace DevHawk.RegistrarTests
{
    public static class Common
    {
        public const string DOMAIN_NAME = "sample.domain";
        public readonly static byte[] DOMAIN_NAME_BYTES = Neo.Utility.StrictUTF8.GetBytes(DOMAIN_NAME);
        public readonly static Lazy<IFileSystem> FILE_SYSTEM = new Lazy<IFileSystem>(() => new FileSystem());
    }
}

