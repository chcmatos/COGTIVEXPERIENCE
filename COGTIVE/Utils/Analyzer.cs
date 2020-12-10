using COGTIVE.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace COGTIVE.Utils
{
    /// <summary>
    /// CSV file data analyzer.
    /// </summary>
    internal sealed class Analyzer : IDisposable
    {
        private readonly object keysLocker, entriesLocker, cancellLocker;

        private DependencyObject visualContext;
        private AnalyzerProgressEventArgs lastProgArgs;
        private IStorageItem storageItem;
        private List<string> keys;
        private List<IReadOnlyList<string>> entries;
        
        private bool cached, noKeys, disposing, disposed, cancelled;
        private long? fileSize;
        private char? separatorChar;
        private int? requestEntryCount;
                
        public event EventHandler<AnalyzerProgressEventArgs> Progress;

        public event EventHandler Done;

        public event EventHandler<Exception> Error;
        
        public event AnalyzerRequestCancelEventHandler Cancel;

        ~Analyzer()
        {
            this.Dispose(false);
        }

        public Analyzer(IStorageItem storageItem)
        {
            this.storageItem = ObjectUtils.RequireNonNull(storageItem);
            this.entries = new List<IReadOnlyList<string>>();
            this.keysLocker = new object();
            this.entriesLocker = new object();
            this.cancellLocker = new object();
        }

        #region Rules
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
            this.RequireNotEnableNoKeys();
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

        public Analyzer SetVisualContext(DependencyObject visualContext)
        {
            this.visualContext = ObjectUtils.RequireNonNull(visualContext);
            return this;
        }
        #endregion

        #region Another Public Actions
        public async Task<int> IndexOfKeyAsync(string key)
        {
            this.RequireNonDisposed();
            this.RequireNotEnableNoKeys();
            await RequireKeysOrCachedAsync();
            return keys.IndexOf(key);
        }

        public async Task<IReadOnlyList<string>> GetEntryAtAsync(int index)
        {
            this.RequireNonDisposed();
            this.RequireCached();
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
            this.RequireNonDisposed();
            return ReadEntriesAsync(entryAction: ObjectUtils.RequireNonNull(consumer));
        }

        public Task LoadCacheAsync()
        {
            this.RequireNonDisposed();

            lock (entriesLocker)
            {
                if (entries.Any())
                {
                    return Task.Delay(0);
                }
            }

            return ReadEntriesAsync();
        }
        
        private long GetFileSize()
        {
            this.RequireNonDisposed();

            if (fileSize.HasValue)
            {
                return fileSize.Value;
            }

            Task<BasicProperties> task = storageItem.GetBasicPropertiesAsync().AsTask();
            task.Wait();
            return (fileSize = Convert.ToInt64(task.Result.Size)).Value;
        }

        public async Task<long> GetFileSizeAsync()
        {
            this.RequireNonDisposed();

            if (fileSize.HasValue)
            {
                return fileSize.Value;
            }

            BasicProperties filesize = await storageItem.GetBasicPropertiesAsync();
            return (fileSize = Convert.ToInt64(filesize.Size)).Value;
        }
        #endregion

        #region ReduceAsync
        public async Task<ACC> ReduceAsync<ACC>(Func<ACC, IReadOnlyList<string>, int, ACC> reduceFun, ACC acc = null)
            where ACC : class
        {
            this.RequireNonDisposed();
            ObjectUtils.RequireNonNull(reduceFun, nameof(reduceFun));
            await this.ReadEntriesAsync(entryAction: (entry, index) => acc = reduceFun.Invoke(acc, entry, index));
            return acc;
        }

        public async Task<ACC> ReduceAsync<ACC, Entry>(Func<IReadOnlyList<string>, Entry> convertEntry, Func<ACC, Entry, int, ACC> reduceFun, ACC acc = null)
            where ACC : class
        {
            this.RequireNonDisposed();
            ObjectUtils.RequireNonNull(convertEntry, nameof(convertEntry));
            ObjectUtils.RequireNonNull(reduceFun, nameof(reduceFun));
            await this.ReadEntriesAsync(entryAction: (entry, index) => acc = reduceFun.Invoke(acc, convertEntry.Invoke(entry), index));
            return acc;
        }
        #endregion

        #region ReadEntries From File or Cached
        private async Task ReadEntriesAsync(bool readEntries = true, Action<IReadOnlyList<string>, int> entryAction = null)
        {
            this.ResetCancel();

            if (cached && entries.Any())
            {
                await this.ReadCacheAsync(readEntries, entryAction);
            }
            else
            {
                await this.ReadFileAsync((entry, index) =>
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
                try
                {
                    int index = 0;
                    long consumed = 0L;
                    if (keys?.Any() ?? false)
                    {
                        lock (keysLocker)
                        {
                            this.RequireNonDisposed();
                            this.RequireNonCancelled();
                            tuple.Item2.Invoke(keys, index++);
                            this.RiseEventProgress(keys, ref consumed);
                            this.RiseEventRequestCancel();
                        }
                    }

                    if (tuple.Item1)
                    {
                        lock (entriesLocker)
                        {
                            foreach (var entry in entries)
                            {
                                this.RequireNonDisposed();
                                this.RequireNonCancelled();
                                tuple.Item2.Invoke(entry, index++);
                                this.RiseEventProgress(keys, ref consumed);
                                this.RiseEventRequestCancel();
                            }
                        }
                    }

                    this.RiseEventDone();
                }
                catch (Exception ex)
                {
                    this.RiseEventErrorOrThrowException(ex);
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
                try
                {
                    using (Stream stream = tuple.Item1)
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        int index = 0;
                        long consumed = 0L;
                        string line;                        
                        while ((line = reader.ReadLine()) != null)
                        {
                            this.RequireNonDisposed();
                            this.RequireNonCancelled();
                            IReadOnlyList<string> entry = this.ToListEntries(line, index);
                            this.RiseEventProgress(line, ref consumed);
                            if (!tuple.Item2.Invoke(entry, index++))
                            {
                                break;
                            }
                            this.RiseEventRequestCancel();
                        }

                        this.RiseEventDone();
                    }
                } 
                catch(Exception ex)
                {
                    this.RiseEventErrorOrThrowException(ex);
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

        private IReadOnlyList<string> ToListEntries(string line, int index)
        {
            if (!separatorChar.HasValue)
            {
                separatorChar = GetIdentifySeparatorCharByLine(line);
            }

            return new List<string>(RequireValidEntryCount(
                line.Split(separatorChar.Value), index)).AsReadOnly();
        }
        #endregion

        #region GetIdentifySeparatorCharByLine
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
        #endregion

        #region Fire Events Callbacks      
        private void FireOnVisualContextOrCurrentAsync(Action consumer, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal, bool isIgnoreDispose = false)
        {
            if (visualContext != null)
            {
                _ = visualContext.Dispatcher.RunAsync(priority, () => 
                {
                    if((!isIgnoreDispose && (disposing || disposed)) || cancelled)
                    {
                        return;
                    }

                    consumer.Invoke();
                });
            } 
            else
            {
                consumer.Invoke();
            }
        }

        #region Progress
        private void RiseEventProgress(List<string> entries, ref long consumed)
        {
            this.RiseEventProgress(() => entries.Sum(System.Text.Encoding.ASCII.GetByteCount), ref consumed);
        }

        private void RiseEventProgress(string readLine, ref long consumed)
        {
            this.RiseEventProgress(() => System.Text.Encoding.ASCII.GetByteCount(readLine), ref consumed);
        }

        private void RiseEventProgress(Func<long> consumedFun, ref long consumed)
        {
            if (Progress != null)
            {
                long fileSize = GetFileSize();
                long cAux = consumed += consumedFun.Invoke();
                var progressAux = Progress;
                var curr = new AnalyzerProgressEventArgs(fileSize, cAux);
                if(lastProgArgs == null || lastProgArgs.Percentage < curr.Percentage)
                {
                    this.FireOnVisualContextOrCurrentAsync(
                    () => progressAux.Invoke(this, lastProgArgs = curr));
                    System.Diagnostics.Debug.WriteLine($"UPDATING PROGRESS {curr.Percentage}");
                }
                //else
                //{
                //    System.Diagnostics.Debug.WriteLine("NOT UPDATE PROGRESS");
                //}   
            }
        }
        #endregion

        private void RiseEventDone()
        {
            if (Done != null)
            {
                var doneAux = Done;
                this.FireOnVisualContextOrCurrentAsync(() => doneAux.Invoke(this, EventArgs.Empty), CoreDispatcherPriority.Low, true);
            }

            this.ResetProgressArgs();
        }

        private void RiseEventErrorOrThrowException(Exception ex)
        {
            this.ResetProgressArgs();

            if (Error != null)
            {
                var errorAux = Error;
                this.FireOnVisualContextOrCurrentAsync(() => errorAux.Invoke(this, ex), CoreDispatcherPriority.High);
            } 
            else
            {
                throw ex;
            }
        }

        private void RiseEventRequestCancel()
        {
            if(Cancel != null)
            {
                FireOnVisualContextOrCurrentAsync(() => 
                {
                    if(Cancel.Invoke(this, EventArgs.Empty))
                    {
                        this.SetCancel(); 
                        this.ResetProgressArgs();
                    }
                });
            }
        }
        #endregion

        #region Require
        private string[] RequireValidEntryCount(string[] entries, int index)
        {
            if (requestEntryCount.HasValue && requestEntryCount.Value != entries.Length)
            {
                throw new IndexOutOfRangeException($"Valores na linha de índice {index} " +
                    $"não contém a quantidade exigidas de {entries.Length} registro(s)!");
            }

            return entries;
        }

        private void RequireNotEnableNoKeys()
        {
            if (noKeys) throw new InvalidOperationException("No Keys!");
        }

        private void RequireCached()
        {
            if (!cached)
            {
                throw new InvalidOperationException("Cache is not enabled!");
            }
        }

        private async Task RequireKeysOrCachedAsync()
        {
            if (keys == null)
            {
                RequireCached();
                await this.ReadEntriesAsync(false);
            }
        }

        private void RequireNonDisposed()
        {
            if(disposed || disposing)
            {
                throw new ObjectDisposedException(nameof(Analyzer));
            }
        }

        private void RequireNonCancelled()
        {
            lock (cancellLocker)
            {
                if (cancelled) throw new OperationCanceledException();
            }
        }

        private void ResetCancel()
        {
            lock(cancellLocker)
            {
                cancelled = false;
            }
        }

        private void SetCancel()
        {
            lock (cancellLocker)
            {
                cancelled = true;
            }
        }

        private void ResetProgressArgs()
        {
            lastProgArgs = null;
        }
        #endregion

        #region IDisposable
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

            this.Progress = null;
            this.Done = null;
            this.Error = null;
            this.Cancel = null;

            this.storageItem = null;
            this.keys = null;
            this.entries = null;
            this.separatorChar = null;
            this.requestEntryCount = null;
            this.fileSize = null;
            this.lastProgArgs = null;
            this.visualContext = null;
            this.disposing = false;
            this.disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
