// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel;
using Nethermind.Core.Attributes;

namespace Nethermind.Merge.Plugin
{
    public static class Metrics
    {
        [GaugeMetric]
        [Description("NewPayload request execution time")]
        public static long NewPayloadExecutionTime { get; set; }

        [GaugeMetric]
        [Description("ForkchoiceUpded request execution time")]
        public static long ForkchoiceUpdedExecutionTime { get; set; }

        [CounterMetric]
        [Description("Number of GetPayload Requests")]
        public static long GetPayloadRequests { get; set; }

        [GaugeMetric]
        [Description("Number of Transactions included in the Last GetPayload Request")]
        public static int NumberOfTransactionsInGetPayload { get; set; }

        [GaugeMetric]
        [Description("Number of Blobs requested by engine_getBlobsV1")]
        public static int NumberOfRequestedBlobs { get; set; }

        [GaugeMetric]
        [Description("Number of Blobs sent by engine_getBlobsV1")]
        public static int NumberOfSentBlobs { get; set; }

        [GaugeMetric]
        [Description("Number of responses to engine_getBlobsV1 with all requested blobs")]
        public static int GetBlobsRequestsSuccessTotal { get; set; }

        [GaugeMetric]
        [Description("Number of responses to engine_getBlobsV1 without all requested blobs")]
        public static int GetBlobsRequestsFailureTotal { get; set; }
    }
}
