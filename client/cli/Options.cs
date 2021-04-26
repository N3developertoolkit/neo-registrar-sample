using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Neo;
using Neo.Wallets;

namespace DevHawk.Registrar.Cli
{
    class Options
    {
        public FileInfo? WalletPath { get; set; }
        public string? PrivateKey { get; set; }

        public bool TryGetKeyPair([MaybeNullWhen(false)] out KeyPair value)
        {
            if (PrivateKey != null)
            {
                value = new KeyPair(PrivateKey.HexToBytes());
                return true;
            }

            if (WalletPath != null)
            {
                throw new NotImplementedException("WalletPath support not implemented");
            }

            value = default;
            return false;
        }
    }
}
