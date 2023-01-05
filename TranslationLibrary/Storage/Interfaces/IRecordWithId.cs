namespace TranslationLibrary.Storage.Interfaces;

public interface IRecordWithId<IdType>
{
    public IdType Id { get; set; }
}