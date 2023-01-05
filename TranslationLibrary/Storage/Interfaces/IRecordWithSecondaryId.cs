namespace TranslationLibrary.Storage.Interfaces;

public interface IRecordWithSecondaryId<IdType>
{
    public IdType SecondaryId { get; set; }
}