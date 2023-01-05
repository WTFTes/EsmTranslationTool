namespace TranslationLibrary.Storage.Interfaces;

public interface IIdGetter<T>
{
    public T Get<TV>(TV record);
}
