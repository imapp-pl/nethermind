// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;

namespace Nethermind.Synchronization.Blocks
{
    [Flags]
    public enum DownloaderOptions
    {
        Insert = 0,
        Process = 1,
        WithReceipts = 2,
    }
}
