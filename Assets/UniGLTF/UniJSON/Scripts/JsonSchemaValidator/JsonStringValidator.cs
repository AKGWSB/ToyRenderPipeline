using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace UniJSON
{
    /// <summary>
    /// http://json-schema.org/latest/json-schema-validation.html#string
    /// </summary>
    public class JsonStringValidator : IJsonSchemaValidator
    {
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.3.1
        /// </summary>
        public int? MaxLength
        {
            get; set;
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.3.2
        /// </summary>
        public int? MinLength
        {
            get; set;
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.3.3
        /// </summary>
        public Regex Pattern
        {
            get; set;
        }

        public override int GetHashCode()
        {
            return 4;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as JsonStringValidator;
            if (rhs == null) return false;

            if (MaxLength != rhs.MaxLength) return false;
            if (MinLength != rhs.MinLength) return false;

            if (Pattern == null && rhs.Pattern == null)
            {
            }
            else if (Pattern == null)
            {
                return false;
            }
            else if (rhs.Pattern == null)
            {
                return false;
            }
            else if (Pattern.ToString() != rhs.Pattern.ToString())
            {
                return false;
            }

            return true;
        }

        public void Assign(IJsonSchemaValidator obj)
        {
            var rhs = obj as JsonStringValidator;
            if (rhs == null)
            {
                throw new ArgumentException();
            }

            MaxLength = rhs.MaxLength;
            MinLength = rhs.MinLength;
            Pattern = rhs.Pattern;
        }

        public bool Parse(IFileSystemAccessor fs, string key, JsonNode value)
        {
            switch (key)
            {
                case "maxLength":
                    MaxLength = value.GetInt32();
                    return true;

                case "minLength":
                    MinLength = value.GetInt32();
                    return true;

                case "pattern":
                    Pattern = new Regex(value.GetString().Replace("\\\\", "\\"));
                    return true;
            }

            return false;
        }

        public JsonSchemaValidationException Validate(JsonSchemaValidationContext c, object o)
        {
            if (o == null)
            {
                return new JsonSchemaValidationException(c, "null");
            }

            var value = o as string;
            if (value.All(x => Char.IsWhiteSpace(x)))
            {
                return new JsonSchemaValidationException(c, "whitespace");
            }

            if (MinLength.HasValue && value.Length < MinLength)
            {
                return new JsonSchemaValidationException(c, string.Format("minlength: {0}<{1}", value.Length, MinLength.Value));
            }
            if (MaxLength.HasValue && value.Length > MaxLength)
            {
                return new JsonSchemaValidationException(c, string.Format("maxlength: {0}>{1}", value.Length, MaxLength.Value));
            }

            if(Pattern!=null && !Pattern.IsMatch(value))
            {
                return new JsonSchemaValidationException(c, string.Format("pattern: {0} not match {1}", Pattern, value));
            }

            return null;
        }

        public void Serialize(JsonFormatter f, JsonSchemaValidationContext c, object o)
        {
            f.Value((string)o);
        }

        public void ToJson(JsonFormatter f)
        {
            f.Key("type"); f.Value("string");
        }
    }
}
