namespace TranslationLibrary.Storage.Interfaces;

public interface IRecordWithSecondaryId<T>
{
    public T GetSecondaryId();
}
