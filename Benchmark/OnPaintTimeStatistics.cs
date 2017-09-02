// Copyright 2011-2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Poderosa.Benchmark {

    internal class OnPaintTimeStatistics {

        private const int MAX_SAMPLES = 1024;

        private long[] _samples = new long[MAX_SAMPLES];
        private int _sampleNextIndex = 0;
        private int _sampleCount = 0;

        private long[] _sortedSamples = null;
        private int _sortedSamplesFrom = -1;
        private int _sortedSamplesTo = -1;

        public OnPaintTimeStatistics() {
        }

        public void Update(Stopwatch s) {
            long ticks = s.ElapsedTicks;
            _samples[_sampleNextIndex] = ticks;
            _sampleNextIndex = (_sampleNextIndex + 1) % MAX_SAMPLES;
            if (_sampleCount < MAX_SAMPLES)
                _sampleCount++;
        }

        public int GetSampleCount() {
            PrepareSortedSamples();
            return _sortedSamplesTo - _sortedSamplesFrom;
        }

        public double GetMinTimeMilliseconds() {
            PrepareSortedSamples();
            if (_sortedSamplesFrom == _sortedSamplesTo)
                return 0.0;

            long minTicks = _sortedSamples[_sortedSamplesFrom];
            return (double)(minTicks * 100000L / Stopwatch.Frequency) / 100.0;
        }

        public double GetMaxTimeMilliseconds() {
            PrepareSortedSamples();
            if (_sortedSamplesFrom == _sortedSamplesTo)
                return 0.0;

            long maxTicks = _sortedSamples[_sortedSamplesTo - 1];
            return (double)(maxTicks * 100000L / Stopwatch.Frequency) / 100.0;
        }

        public double GetAverageTimeMilliseconds() {
            PrepareSortedSamples();
            if (_sortedSamplesFrom == _sortedSamplesTo)
                return 0.0;

            long sumTicks = 0;
            for(int i = _sortedSamplesFrom; i < _sortedSamplesTo; i++) {
                sumTicks += _sortedSamples[i];
            }
            return (double)(sumTicks * 100000L / (Stopwatch.Frequency * (_sortedSamplesTo - _sortedSamplesFrom))) / 100.0;
        }


        private void PrepareSortedSamples() {
            if (_sortedSamples == null) {
                _sortedSamples = GetSortedSampleArray();

                if (_sortedSamples.Length == 0) {
                    _sortedSamplesFrom = _sortedSamplesTo = 0;
                }
                else {
                    _sortedSamplesFrom = _sortedSamples.Length / 10;    // ignore short cases
                    _sortedSamplesTo = _sortedSamples.Length;
                }
            }
        }

        private long[] GetSortedSampleArray() {
            long[] sampleArray = GetSampleArray();
            Array.Sort<long>(sampleArray);
            return sampleArray;
        }

        private long[] GetSampleArray() {
            if (_sampleCount == MAX_SAMPLES)
                return (long[])_samples.Clone();
            long[] sampleArray = new long[_sampleCount];
            Buffer.BlockCopy(_samples, 0, sampleArray, 0, _sampleCount * sizeof(long));
            return sampleArray;
        }
    }
}
