using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Converters
{
    internal class OperationJsonConverter<TModel> : BaseOperationJsonConverter<Operation<TModel>> where TModel : class
    {
        protected override Operation<TModel> CreateInstance(string op, string path, string? from, object? value)
        {
            return new(op, path, from, value);
        }
    }
}
