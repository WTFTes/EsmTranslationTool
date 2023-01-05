namespace TranslationLibrary.Storage.Interfaces;

public interface IRecordWithContext<ContextType>
{
    public ContextType ContextName { get; set; }
}