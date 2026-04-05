namespace Mytril.Audit.Domain.Services;

public interface IPayloadSanitizer
{
    bool HasForbiddenFields(string json);
}
