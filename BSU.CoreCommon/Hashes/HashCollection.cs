using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BSU.CoreCommon.Hashes;

public interface IHashCollection
{
    Task<IModHash> GetHash(Type type);
    List<Type> GetSupportedHashTypes();
}

public class HashCollection : IHashCollection
{
    // Immutable
    
    private readonly List<IModHash> _hashes;

    public HashCollection(params IModHash[] hashes)
    {
        _hashes = hashes.ToList();
    }

    public Task<IModHash> GetHash(Type type) => Task.FromResult(_hashes.Single(h => h.GetType() == type));

    public List<Type> GetSupportedHashTypes() => _hashes.Select(h => h.GetType()).ToList();

    public ReadOnlyCollection<IModHash> GetAll() => new(_hashes);
}
