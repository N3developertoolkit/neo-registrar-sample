using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;

namespace DevHawk.Contracts
{
    class DomainStorage
    {
        readonly StorageMap storageMap;

        public DomainStorage(byte prefix)
        {
            storageMap = new StorageMap(Storage.CurrentContext, prefix);
        }

        public UInt160 Get(string domain) => (UInt160)storageMap.Get(domain) ?? UInt160.Zero;
        public void Put(string domain, UInt160 owner) => storageMap.Put(domain, (ByteString)owner);
        public void Delete(string domain) => storageMap.Delete(domain);
    }
}
