// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Trie.Pruning
{
    public interface IPruningStrategy
    {
        bool DeleteObsoleteKeys { get; }
        bool ShouldPruneDirtyNode(TrieStoreState state);
        bool ShouldPrunePersistedNode(TrieStoreState state);
    }
}
