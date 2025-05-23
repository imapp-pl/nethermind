// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Crypto;

namespace Nethermind.Evm.Precompiles
{
    public class EcRecoverPrecompile : IPrecompile<EcRecoverPrecompile>
    {
        public static readonly EcRecoverPrecompile Instance = new();

        private EcRecoverPrecompile()
        {
        }

        public static Address Address { get; } = Address.FromNumber(1);

        public long DataGasCost(ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec) => 0L;

        public long BaseGasCost(IReleaseSpec releaseSpec) => 3000L;

        private readonly EthereumEcdsa _ecdsa = new(BlockchainIds.Mainnet);

        private readonly byte[] _zero31 = new byte[31];

        public (byte[], bool) Run(ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec)
        {
            Metrics.EcRecoverPrecompile++;

            Span<byte> inputDataSpan = stackalloc byte[128];
            inputData.Span[..Math.Min(128, inputData.Length)]
                .CopyTo(inputDataSpan[..Math.Min(128, inputData.Length)]);

            Hash256 hash = new(inputDataSpan[..32]);
            Span<byte> vBytes = inputDataSpan.Slice(32, 32);
            Span<byte> r = inputDataSpan.Slice(64, 32);
            Span<byte> s = inputDataSpan.Slice(96, 32);

            // TEST: CALLCODEEcrecoverV_prefixedf0_d0g0v0
            // TEST: CALLCODEEcrecoverV_prefixedf0_d1g0v0
            if (!Bytes.AreEqual(_zero31, vBytes[..31]))
            {
                return ([], true);
            }

            byte v = vBytes[31];
            if (v != 27 && v != 28)
            {
                return ([], true);
            }

            Signature signature = new(r, s, v);
            Address recovered = _ecdsa.RecoverAddress(signature, hash);
            if (recovered is null)
            {
                return ([], true);
            }

            byte[] result = recovered.Bytes;
            if (result.Length != 32)
            {
                result = result.PadLeft(32);
            }

            // TODO: change recovery code to return bytes
            return (result, true);
        }
    }
}
