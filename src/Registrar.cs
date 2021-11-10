using System;
using System.ComponentModel;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace DevHawk.Contracts
{
    [DisplayName("DevHawk.Registrar")]
    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "harrypierson@hotmail.com")]
    [ManifestExtra("Description", "This is an example contract")]
    public class Registrar : SmartContract
    {
        readonly DomainStorage domainOwners = new DomainStorage(nameof(domainOwners));

        public UInt160 Query(string domain)
        {
            var currentOwner = domainOwners.Get(domain);
            if (currentOwner.IsZero)
            {
                Runtime.Log("Domain not registered");
            }

            return currentOwner;
        }

        public bool Register(string domain, UInt160 owner)
        {
            var currentOwner = domainOwners.Get(domain);
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

            domainOwners.Put(domain, owner);
            return true;
        }

        public bool Transfer(string domain, UInt160 to)
        {
            var currentOwner = domainOwners.Get(domain);
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

            domainOwners.Put(domain, to);
            return true;
        }

        public bool Delete(string domain)
        {
            var currentOwner = domainOwners.Get(domain);
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

            domainOwners.Delete(domain);
            return true;
        }

        [DisplayName("_deploy")]
        public void Deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            Storage.Put(Storage.CurrentContext, nameof(Registrar), tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            var tx = (Transaction)Runtime.ScriptContainer;
            var contractOwner = Storage.Get(Storage.CurrentContext, nameof(Registrar));
            if (!contractOwner.Equals(tx.Sender))
            {
                throw new Exception("Only the contract owner can update the contract");
            }
            ContractManagement.Update(nefFile, manifest, null);
        }
    }

    class DomainStorage
    {
        readonly StorageMap storageMap;

        public DomainStorage(string prefix)
        {
            storageMap = new StorageMap(Storage.CurrentContext, prefix);
        }

        public UInt160 Get(string domain) => (UInt160)storageMap.Get(domain) ?? UInt160.Zero;
        public void Put(string domain, UInt160 owner) => storageMap.Put(domain, (ByteString)owner);
        public void Delete(string domain) => storageMap.Delete(domain);
    }
}
