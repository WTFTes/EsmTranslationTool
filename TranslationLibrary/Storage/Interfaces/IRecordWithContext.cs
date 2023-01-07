namespace TranslationLibrary.Storage.Interfaces;

public interface IRecordWithContext<T>
{
    public T GetContext();
}
