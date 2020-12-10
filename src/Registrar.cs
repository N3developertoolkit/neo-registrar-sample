using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

namespace DevHawk.Contracts
{
    [DisplayName("DevHawk.Registrar")]
    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "harrypierson@hotmail.com")]
    [ManifestExtra("Description", "This is an example contract")]
    public class Registrar : SmartContract
    {
        public static UInt160 Query(string domain)
        {
            return UInt160.Zero;
            // return Storage.Get(Storage.CurrentContext, domain);
        }

        public static bool Register(string domain, UInt160 owner)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            byte[] value = Storage.Get(Storage.CurrentContext, domain);
            if (value != null) return false;
            Storage.Put(Storage.CurrentContext, domain, (byte[])owner);
            return true;
        }

        public static bool Transfer(string domain, UInt160 to)
        {
            // if (!Runtime.CheckWitness(to)) return false;
            // byte[] from = Storage.Get(Storage.CurrentContext, domain);
            // if (from == null) return false;
            // if (!Runtime.CheckWitness(from)) return false;
            // Storage.Put(Storage.CurrentContext, domain, to);
            return true;
        }

        public static bool Delete(string domain)
        {
            var owner = (UInt160)Storage.Get(Storage.CurrentContext, domain);
            if (owner == null) return false;
            if (!Runtime.CheckWitness(owner)) return false;
            Storage.Delete(Storage.CurrentContext, domain);
            return true;
        }
    }
}
