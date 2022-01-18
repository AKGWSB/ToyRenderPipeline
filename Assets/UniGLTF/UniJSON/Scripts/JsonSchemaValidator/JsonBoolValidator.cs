using System;


namespace UniJSON
{
    public class JsonBoolValidator : IJsonSchemaValidator
    {
        public override int GetHashCode()
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as JsonBoolValidator;
            if (rhs == null) return false;
            return true;
        }

        public void Assign(IJsonSchemaValidator obj)
        {
            throw new NotImplementedException();
        }

        public bool Parse(IFileSystemAccessor fs, string key, JsonNode value)
        {
            return false;
        }

        public JsonSchemaValidationException Validate(JsonSchemaValidationContext c, object o)
        {
            return null;
        }

        public void Serialize(JsonFormatter f, JsonSchemaValidationContext c, object o)
        {
            f.Value((bool)o);
        }

        public void ToJson(JsonFormatter f)
        {
            f.Key("type"); f.Value("boolean");
        }
    }
}
