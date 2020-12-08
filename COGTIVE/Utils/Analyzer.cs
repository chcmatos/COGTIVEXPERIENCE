using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace COGTIVE.Utils
{
    public sealed class Analyzer : IDisposable
    {
        private IStorageItem storageItem;
        private List<string> keys;
        private List<IReadOnlyList<string>> entries;
        private readonly object keysLocker, entriesLocker;

        private char? separatorChar;
        private int? requestEntryCount;
        private bool cached, noKeys, disposing, disposed;

        public Analyzer(IStorageItem storageItem)
        {
            this.storageItem = ObjectUtils.RequireNonNull(storageItem);
            this.entries = new List<IReadOnlyList<string>>();
            this.keysLocker = new object();
            this.entriesLocker = new object();
        }

        public Analyzer SetSeparatorChar(char separatorChar)
        {
            if (separatorChar == '\0') throw new InvalidOperationException();
            this.separatorChar = separatorChar;
            return this;
        }

        public Analyzer UseCached()
        {
            this.cached = true;
            return this;
        }

        private Analyzer UseNoKeys()
        {
            this.noKeys = true;
            return this;
        }

        public Analyzer SetKeys(params string[] keys)
        {
            if(keys.Length == 0) throw new ArgumentNullException();
            this.RequestNotEnableNoKeys();
            lock (keysLocker)
            {
                this.keys = new List<string>(keys);
                return this.SetRequestEntryCount(keys.Length);
            }
        }

        public Analyzer SetRequestEntryCount(int count)
        {
            if (count <= 0) throw new IndexOutOfRangeException();
            this.requestEntryCount = count;
            return this;
        }

        public async Task<int> IndexOfKeyAsync(string key)
        {
            RequestNotEnableNoKeys();
            await RequestKeysOrCachedAsync();
            return keys.IndexOf(key);
        }

        public async Task<IReadOnlyList<string>> GetEntryAtAsync(int index)
        {
            RequestCached();
            bool attemptToLoad = false;

            checkEntries:
            lock (entriesLocker)
            {
                if (entries.Any())
                {
                    return entries.ElementAtOrDefault(index);
                } 
                else if(attemptToLoad)
                {
                    return default;
                }
            }

            await ReadEntriesAsync();
            attemptToLoad = true;
            goto checkEntries;
        }

        public Task ForEachAsync(Action<IReadOnlyList<string>, int> consumer)
        {
            return ReadEntriesAsync(entryAction: ObjectUtils.RequireNonNull(consumer));
        }

        public Task LoadCacheAsync()
        {
            lock (entriesLocker)
            {
                if (entries.Any())
                {
                    return Task.Delay(0);
                }
            }

            return ReadEntriesAsync();
        }

        public async Task<ACC> ReduceAsync<ACC>(Func<ACC, IReadOnlyList<string>, int, ACC> reduceFun, ACC acc = null)
            where ACC : class
        {
            ObjectUtils.RequireNonNull(reduceFun, nameof(reduceFun));
            await ReadEntriesAsync(entryAction: (entry, index) => acc = reduceFun.Invoke(acc, entry, index));
            return acc;
        }

        public async Task<ACC> ReduceAsync<ACC, Entry>(Func<IReadOnlyList<string>, Entry> convertEntry, Func<ACC, Entry, int, ACC> reduceFun, ACC acc = null)
            where ACC : class
        {
            ObjectUtils.RequireNonNull(convertEntry, nameof(convertEntry));
            ObjectUtils.RequireNonNull(reduceFun, nameof(reduceFun));
            await ReadEntriesAsync(entryAction: (entry, index) => acc = reduceFun.Invoke(acc, convertEntry.Invoke(entry), index));
            return acc;
        }

        private IReadOnlyList<string> ToListEntries(string line, int index) 
        {
            if (!separatorChar.HasValue)
            {
                separatorChar = GetIdentifySeparatorCharByLine(line);
            }

            return new List<string>(RequestValidEntryCount(
                line.Split(separatorChar.Value), index)).AsReadOnly();
        }

        private string[] RequestValidEntryCount(string[] entries, int index)
        {
            if(requestEntryCount.HasValue && requestEntryCount.Value != entries.Length)
            {
                throw new IndexOutOfRangeException($"Valores na linha de índice {index} " +
                    $"não contém a quantidade exigidas de {entries.Length} registro(s)!");
            }

            return entries;
        }

        private void RequestNotEnableNoKeys()
        {
            if (noKeys) throw new InvalidOperationException("No Keys!");
        }

        private void RequestCached()
        {
            if (!cached)
            {
                throw new InvalidOperationException("Cache is not enabled!");
            }
        }

        private async Task RequestKeysOrCachedAsync()
        {
            if(keys == null)
            {
                RequestCached();
                await this.ReadEntriesAsync(false);
            }
        }

        private async Task ReadEntriesAsync(bool readEntries = true, Action<IReadOnlyList<string>, int> entryAction = null)
        {
            if (cached && entries.Any())
            {
                await ReadCacheAsync(readEntries, entryAction);
            }
            else
            {
                await ReadFileAsync((entry, index) =>
                {
                    entryAction?.Invoke(entry, index);
                    if (cached)
                    {
                        lock (keysLocker)
                        {
                            if (keys == null || !keys.Any())
                            {
                                keys = new List<string>(entry);
                                return readEntries;
                            }
                        }

                        if (readEntries)
                        {
                            lock (entriesLocker)
                            {
                                entries.Add(entry);
                            }
                        }
                    }
                    return readEntries;
                });
            }
        }
        
        private Task ReadCacheAsync(bool readEntries = true, Action<IReadOnlyList<string>, int> entryAction = null)
        {
            return Task.Factory.StartNew(ReadEntriesFromCache, Tuple.Create(readEntries, entryAction));
        }

        private void ReadEntriesFromCache(object state)
        {
            if (state is Tuple<bool, Action<IReadOnlyList<string>, int>> tuple && tuple.Item2 != null)
            {
                int index = 0;
                if (keys?.Any() ?? false)
                {
                    lock (keysLocker)
                    {
                        tuple.Item2.Invoke(keys, index++);
                    }
                }

                if (tuple.Item1)
                {
                    lock (entriesLocker)
                    {
                        foreach(var entry in entries)
                        {
                            tuple.Item2.Invoke(entry, index++);
                        }
                    }
                }
            }
        }

        private async Task ReadFileAsync(Func<IReadOnlyList<string>, int, bool> entryAction)
        {
            await Task.Factory.StartNew(ReadEntriesFromFile, 
                Tuple.Create(await OpenStreamAsync(), entryAction));
        }

        private void ReadEntriesFromFile(object state)
        {
            if (state is Tuple<Stream, Func<IReadOnlyList<string>, int, bool>> tuple)
            {
                using (Stream stream = tuple.Item1)
                using (StreamReader reader = new StreamReader(stream))
                {
                    int index = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        IReadOnlyList<string> entry = ToListEntries(line, index);
                        if (!tuple.Item2.Invoke(entry, index++))
                        {
                            break;
                        }
                    }
                }
            }
        }

        private async Task<Stream> OpenStreamAsync()
        {
            if (storageItem is StorageFile storageFile)
            {
                return await storageFile.OpenStreamForReadAsync();
            }
            else
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                storageFile = await storageFolder.GetFileAsync(storageItem.Path);
                return await storageFile.OpenStreamForReadAsync();
            }
        }

        private char GetIdentifySeparatorCharByLine(string line, params char[] chars)
        {
            int len = ObjectUtils.RequireNonNullOrEmpty(line).Length;

            int[] cArr = new int[chars.Length];
            int maxi = 0;
            for (int i = 0, l = chars.Length; i < len; i++)
            {
                char c = line[i];
                for (int j = 0; j < l; j++)
                {
                    if (c == chars[j])
                    {
                        int cc = ++cArr[j];
                        if (maxi != j && cArr[maxi] < cc)
                        {
                            maxi = j;
                        }
                    }
                }
            }

            return chars[maxi];
        }

        private char GetIdentifySeparatorCharByLine(string line)
        {
            return GetIdentifySeparatorCharByLine(line, ';', ',');
        }

        private void Dispose(bool isDisposing)
        {
            if (disposed || disposing)
            {
                return;
            }
            else if (disposing = isDisposing)
            {
                lock (entriesLocker)
                {
                    entries?.Clear();
                }

                lock (keysLocker)
                {
                    keys?.Clear();
                }
            }

            this.storageItem = null;
            this.keys = null;
            this.entries = null;
            this.separatorChar = null;
            this.requestEntryCount = null;
            this.disposing = false;
            this.disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
