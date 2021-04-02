using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Converters
{
    internal class OperationJsonConverter : BaseOperationJsonConverter<Operation>
    {
        protected override Operation CreateInstance(string op, string path, string? from, object? value)
        {
            return new(op, path, from, value);
        }
    }
}
