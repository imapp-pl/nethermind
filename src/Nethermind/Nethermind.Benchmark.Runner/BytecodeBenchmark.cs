// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Ethereum.Test.Base;
using Nethermind.Config;
using Nethermind.Consensus.Validators;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test;
using Nethermind.Core.Test.Builders;
using Nethermind.Core.Test.Modules;
using Nethermind.Crypto;
using Nethermind.Db;
using Nethermind.Evm;
using Nethermind.Evm.CodeAnalysis;
using Nethermind.Evm.Tracing;
using Nethermind.Evm.TransactionProcessing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Specs;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;
using Nethermind.State;
using Nethermind.Trie;
using Nethermind.Trie.Pruning;
using NSubstitute.Routing.AutoValues;
using static Nethermind.Evm.VirtualMachine;

namespace Nethermind.Benchmark.Runner;

public class BytecodeBenchmark
{
    public static byte[] ByteCode { get; set; }

    private IReleaseSpec _spec = MainnetSpecProvider.Instance.GetSpec(
        MainnetSpecProvider.OsakaActivation
    );
    private ITxTracer _txTracer = NullTxTracer.Instance;
    private ExecutionEnvironment _environment;
    private IVirtualMachine _virtualMachine;
    private BlockHeader _header = new BlockHeader(
        Keccak.Zero,
        Keccak.Zero,
        Address.Zero,
        UInt256.One,
        MainnetSpecProvider.ParisBlockNumber + 4,
        Int64.MaxValue,
        MainnetSpecProvider.OsakaBlockTimestamp,
        Bytes.Empty
    );
    private IBlockhashProvider _blockhashProvider =
        new Nethermind.Evm.Benchmark.TestBlockhashProvider(MainnetSpecProvider.Instance);
    private EvmState _evmState;
    private IWorldState _stateProvider;

    [GlobalSetup]
    public void GlobalSetup()
    {
        ByteCode = Bytes.FromHexString(
            Environment.GetEnvironmentVariable("NETH.BENCHMARK.BYTECODE") ?? string.Empty
        );

        IWorldStateManager worldStateManager = TestWorldStateFactory.CreateForTest();
        _stateProvider = worldStateManager.GlobalWorldState;
        _stateProvider.CreateAccount(Address.Zero, 1000.Ether());
        _stateProvider.Commit(_spec);
        CodeInfoRepository codeInfoRepository = new();
        _virtualMachine = new VirtualMachine(
            _blockhashProvider,
            MainnetSpecProvider.Instance,
            LimboLogs.Instance
        );
        _virtualMachine.SetBlockExecutionContext(new BlockExecutionContext(_header, _spec));
        _virtualMachine.SetTxExecutionContext(
            new TxExecutionContext(Address.Zero, codeInfoRepository, null, 0)
        );

        KzgPolynomialCommitments.InitializeAsync().Wait();
        _environment = new ExecutionEnvironment(
            executingAccount: Address.Zero,
            codeSource: Address.Zero,
            caller: Address.Zero,
            codeInfo: new CodeInfo(ByteCode),
            callDepth: 0,
            value: 0,
            transferValue: 0,
            inputData: default
        );
    }

    [IterationSetup]
    public void Setup()
    {
        _evmState = EvmState.RentTopLevel(
            long.MaxValue,
            ExecutionType.TRANSACTION,
            _environment,
            new StackAccessTracker(),
            _stateProvider.TakeSnapshot()
        );
    }

    [Benchmark]
    public void ExecuteCode()
    {
        var ts = _virtualMachine.ExecuteTransaction<OffFlag>(_evmState, _stateProvider, _txTracer);
        if (ts.IsError)
        {
            throw new Exception("Execution failed: " + ts.Error);
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _stateProvider.Reset();
    }
}
