namespace BSU.Core.Model
{
    internal interface IModel : IModelStructure
    {
        void DeleteRepository(IModelRepository repository, bool removeMods);
        void DeleteStorage(IModelStorage storage, bool removeMods);
    }
}