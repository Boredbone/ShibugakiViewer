using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ImageLibrary.Creation
{
    public interface IFolderContainer
    {
        
        ImageFileInformation GetImage(string path, PropertiesLevel level);


        Task<long> EnumerateFilesAsync
            (
            Action<int> OnFileEnumerated, 
            bool containsChildren = true, 
            CancellationToken cancellationToken = default(CancellationToken),
            bool configureAwait = false);

        Task<long> EnumerateFilesAsync
            (string[] path,
            CancellationToken cancellationToken = default(CancellationToken), bool configureAwait = false);

        Task<IEnumerable<T>> DoForAllFilesAsync<T>(Func<string, T> action,
            CancellationToken cancellationToken = default(CancellationToken),
            bool configureAwait = false);
    }
}