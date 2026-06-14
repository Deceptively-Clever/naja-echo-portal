namespace NajaEcho.Application.Features.Items.ImportItems;

public sealed record CategoryImportError(int CategoryUexId, string? CategoryName, string Message);
