using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WWControls.SampleApp.SampleData
{
    public static class SampleDataGenerator
    {
        public static List<T> Generate<T>(int count, Func<Random, int, T> producer, int? seed = null)
        {
            var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
            var list = new List<T>(count);
            for (int i = 0; i < count; i++) list.Add(producer(rnd, i));
            return list;
        }

        public static async Task<List<T>> GenerateAsync<T>(
            int count,
            Func<Random, int, T> producer,
            int? seed = null,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0) return new List<T>();

            var generated = new List<T>(count);
            int batchSize = Math.Max(100, count / 50);
            int remaining = count;

            progress?.Report($"Generating 0 / {count:N0}...");

            await Task.Run(() =>
            {
                while (remaining > 0 && !cancellationToken.IsCancellationRequested)
                {
                    int thisBatch = Math.Min(batchSize, remaining);
                    int offset = generated.Count;

                    var batch = new T[thisBatch];
                    Parallel.For(0, thisBatch, i =>
                    {
                        var r = seed.HasValue
                            ? new Random(seed.Value + offset + i)
                            : new Random(Guid.NewGuid().GetHashCode());
                        batch[i] = producer(r, offset + i);
                    });

                    generated.AddRange(batch);
                    remaining -= thisBatch;

                    int done = generated.Count;
                    progress?.Report(done < count
                        ? $"Generating {done:N0} / {count:N0}..."
                        : $"Loading {done:N0} items...");
                }
            }, cancellationToken).ConfigureAwait(false);

            return generated;
        }
    }
}
