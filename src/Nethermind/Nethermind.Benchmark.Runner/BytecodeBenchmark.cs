// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using BenchmarkDotNet.Attributes;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test;
using Nethermind.Core.Test.Builders;
using Nethermind.Db;
using Nethermind.Evm.CodeAnalysis;
using Nethermind.Specs;
using Nethermind.Evm.Tracing;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.Trie;
using Nethermind.Trie.Pruning;
using Nethermind.Evm;
using Ethereum.Test.Base;
using static Nethermind.Evm.VirtualMachine;
using NSubstitute.Routing.AutoValues;


using System.Collections.Generic;
using System.Diagnostics;
using Nethermind.Config;
using Nethermind.Consensus.Validators;
using Nethermind.Core.Test.Modules;
using Nethermind.Crypto;
using Nethermind.Evm.TransactionProcessing;
using Nethermind.Specs.Forks;
using Nethermind.Specs.Test;

namespace Nethermind.Benchmark.Runner;

public class BytecodeBenchmark
{
    public static byte[] ByteCode { get; set; }

    private IReleaseSpec _spec = MainnetSpecProvider.Instance.GetSpec(MainnetSpecProvider.CancunActivation);
    private ITxTracer _txTracer = NullTxTracer.Instance;
    private ExecutionEnvironment _environment;
    private IVirtualMachine _virtualMachine;
    private BlockHeader _header = new BlockHeader(Keccak.Zero, Keccak.Zero, Address.Zero, UInt256.One, MainnetSpecProvider.IstanbulBlockNumber, Int64.MaxValue, 1UL, Bytes.Empty);
    private IBlockhashProvider _blockhashProvider = new TestBlockhashProvider();
    private EvmState _evmState;
    private WorldState _stateProvider;

    [GlobalSetup]
    public void GlobalSetup()
    {
        TrieStore trieStore = TestTrieStoreFactory.Build(new MemDb(),
                Prune.WhenCacheReaches(1.MiB()),
                Persist.EveryNBlock(2), NullLogManager.Instance);
        var codeDb = new MemDb();

        _stateProvider = new WorldState(trieStore, codeDb, new OneLoggerLogManager(NullLogger.Instance));
        _stateProvider.CreateAccount(Address.Zero, 1000.Ether());
        _stateProvider.Commit(_spec);

        _virtualMachine = new VirtualMachine(_blockhashProvider, MainnetSpecProvider.Instance, LimboLogs.Instance);

        
    }

    [IterationSetup]
    public void Setup()
    {
        ByteCode = Bytes.FromHexString(Environment.GetEnvironmentVariable("NETH.BENCHMARK.BYTECODE") ?? string.Empty);
        CodeInfoRepository codeInfoRepository = new();
        KzgPolynomialCommitments.InitializeAsync().Wait();
        
        _environment = new ExecutionEnvironment
        (
            executingAccount: Address.Zero,
            codeSource: Address.Zero,
            caller: Address.Zero,
            codeInfo: new CodeInfo(ByteCode),
            value: 0,
            transferValue: 0,
            txExecutionContext: new TxExecutionContext(new BlockExecutionContext(_header, _spec), Address.Zero, 0, null, codeInfoRepository),
            inputData: default
        );

        _evmState = EvmState.RentTopLevel(long.MaxValue, ExecutionType.TRANSACTION, _stateProvider.TakeSnapshot(), _environment, new StackAccessTracker());
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