namespace NajaEcho.Application.Features.Locations.ImportLocations;

public sealed class EmptySourceException : Exception
{
    public EmptySourceException(string entityName)
        : base($"The UEX source returned an empty record set for {entityName}; import aborted.")
    {
    }
}
