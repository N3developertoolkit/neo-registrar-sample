using Neo;

namespace DevHawk
{
    // this interface will eventually be generated from contract manifest ABI
    interface Registrar
    {
        UInt160 query(string domain);
        bool register(string domain, UInt160 owner);
        bool transfer(string domain, UInt160 to);
        bool delete(string domain);
    }
}

