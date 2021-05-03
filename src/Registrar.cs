using System.ComponentModel;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;

namespace DevHawk.Contracts
{
    [DisplayName("DevHawk.Registrar")]
    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "harrypierson@hotmail.com")]
    [ManifestExtra("Description", "This is an example contract")]
    public class Registrar : SmartContract
    {
        static UInt160 GetDomainOwner(string domain)
        {
            var value = Storage.Get(Storage.CurrentContext, domain);
            return (value == null) ? UInt160.Zero : (UInt160)value;
        }

        public UInt160 Query(string domain)
        {
            var currentOwner = GetDomainOwner(domain);
            if (currentOwner.IsZero)
            {
                Runtime.Log("Domain not registered");
            }

            return currentOwner;
        }

        public bool Register(string domain, UInt160 owner)
        {
            var currentOwner = GetDomainOwner(domain);
            if (!currentOwner.IsZero)
            {
                Runtime.Log("Domain already registered");
                return false;
            }
            if (!Runtime.CheckWitness(owner))
            {
                Runtime.Log("CheckWitness Failed");
                return false;
            }

            Storage.Put(Storage.CurrentContext, domain, (ByteString)owner);
            return true;
        }

        public bool Transfer(string domain, UInt160 to)
        {
            var currentOwner = GetDomainOwner(domain);
            if (currentOwner.IsZero)
            {
                Runtime.Log("Domain not registered");
                return false;
            }
            if (!Runtime.CheckWitness(currentOwner))
            {
                Runtime.Log("CheckWitness failed for current owner");
                return false;
            }
            if (!Runtime.CheckWitness(to))
            {
                Runtime.Log("CheckWitness failed for receiver");
                return false;
            }

            Storage.Put(Storage.CurrentContext, domain, (ByteString)to);
            return true;
        }

        public bool Delete(string domain)
        {
            var currentOwner = GetDomainOwner(domain);
            if (currentOwner.IsZero)
            {
                Runtime.Log("Domain not registered");
                return false;
            }
            if (!Runtime.CheckWitness(currentOwner))
            {
                Runtime.Log("CheckWitness failed for current owner");
                return false;
            }

            Storage.Delete(Storage.CurrentContext, domain);
            return true;
        }
    }
}
