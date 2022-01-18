using System;
using System.Linq;
using System.Collections.Generic;


namespace UniJSON
{
    /// <summary>
    /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5
    /// </summary>
    public class JsonObjectValidator : IJsonSchemaValidator
    {
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.1
        /// </summary>
        public int MaxProperties
        {
            get; set;
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.2
        /// </summary>
        public int MinProperties
        {
            get; set;
        }

        List<string> m_required = new List<string>();
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.3
        /// </summary>
        public List<string> Required
        {
            get { return m_required; }
        }

        Dictionary<string, JsonSchema> m_props;
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.4
        /// </summary>
        public Dictionary<string, JsonSchema> Properties
        {
            get
            {
                if (m_props == null)
                {
                    m_props = new Dictionary<string, JsonSchema>();
                }
                return m_props;
            }
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.5
        /// </summary>
        public string PatternProperties
        {
            get; private set;
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.6
        /// </summary>
        public JsonSchema AdditionalProperties
        {
            get; set;
        }

        Dictionary<string, string[]> m_depndencies;
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.7
        /// </summary>
        public Dictionary<string, string[]> Dependencies
        {
            get
            {
                if (m_depndencies == null)
                {
                    m_depndencies = new Dictionary<string, string[]>();
                }
                return m_depndencies;
            }
        }

        public void AddProperty(IFileSystemAccessor fs, string key, JsonNode value)
        {
            var sub = new JsonSchema();
            sub.Parse(fs, value, key);

            if (Properties.ContainsKey(key))
            {
                if (sub.Validator != null)
                {
                    Properties[key].Validator.Assign(sub.Validator);
                }
            }
            else
            {
                Properties.Add(key, sub);
            }
        }

        public override int GetHashCode()
        {
            return 6;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as JsonObjectValidator;
            if (rhs == null)
            {
                return false;
            }

            if (Properties.Count != rhs.Properties.Count)
            {
                return false;
            }
            foreach (var pair in Properties)
            {
                JsonSchema value;
                if (rhs.Properties.TryGetValue(pair.Key, out value))
                {
#if true
                    if (!value.Equals(pair.Value))
                    {
                        Console.WriteLine(string.Format("{0} is not equals", pair.Key));
                        var l = pair.Value.Validator;
                        var r = value.Validator;
                        return false;
                    }
#else
                    // key name match
                    return true;
#endif
                }
                else
                {
                    return false;
                }
            }

            if (Required.Count != rhs.Required.Count)
            {
                return false;
            }
            if (!Required.OrderBy(x => x).SequenceEqual(rhs.Required.OrderBy(x => x)))
            {
                return false;
            }

            if (Dependencies.Count != rhs.Dependencies.Count)
            {
                return false;
            }
            foreach (var kv in Dependencies)
            {
                if (!kv.Value.OrderBy(x => x).SequenceEqual(rhs.Dependencies[kv.Key].OrderBy(x => x)))
                {
                    return false;
                }
            }

            if (AdditionalProperties == null
                && rhs.AdditionalProperties == null)
            {
                // ok
            }
            else if (AdditionalProperties == null)
            {
                return false;
            }
            else if (rhs.AdditionalProperties == null)
            {
                return false;
            }
            else
            {
                if (!AdditionalProperties.Equals(rhs.AdditionalProperties))
                {
                    return false;
                }
            }

            return true;
        }

        public void Assign(IJsonSchemaValidator obj)
        {
            var rhs = obj as JsonObjectValidator;
            if (rhs == null)
            {
                throw new ArgumentException();
            }

            foreach (var x in rhs.Properties)
            {
                if (this.Properties.ContainsKey(x.Key))
                {
                    this.Properties[x.Key] = x.Value;
                }
                else
                {
                    this.Properties.Add(x.Key, x.Value);
                }
            }

            foreach (var x in rhs.Required)
            {
                this.Required.Add(x);
            }

            if (rhs.AdditionalProperties != null)
            {
                if (AdditionalProperties != null)
                {
                    throw new NotImplementedException();
                }
                AdditionalProperties = rhs.AdditionalProperties;
            }
        }

        public bool Parse(IFileSystemAccessor fs, string key, JsonNode value)
        {
            switch (key)
            {
                case "maxProperties":
                    MaxProperties = value.GetInt32();
                    return true;

                case "minProperties":
                    MinProperties = value.GetInt32();
                    return true;

                case "required":
                    {
                        foreach (var req in value.ArrayItems)
                        {
                            m_required.Add(req.GetString());
                        }
                    }
                    return true;

                case "properties":
                    {
                        foreach (var prop in value.ObjectItems)
                        {
                            AddProperty(fs, prop.Key, prop.Value);
                        }
                    }
                    return true;

                case "patternProperties":
                    PatternProperties = value.GetString();
                    return true;

                case "additionalProperties":
                    {
                        var sub = new JsonSchema();
                        sub.Parse(fs, value, "additionalProperties");
                        AdditionalProperties = sub;
                    }
                    return true;

                case "dependencies":
                    {
                        foreach (var kv in value.ObjectItems)
                        {
                            Dependencies.Add(kv.Key, kv.Value.ArrayItems.Select(x => x.GetString()).ToArray());
                        }
                    }
                    return true;

                case "propertyNames":
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

            if (Properties.Count == 0)
            {
                return new JsonSchemaValidationException(c, "no properties");
            }

            if (Required != null)
            {
                foreach (var x in Required)
                {
                    using (c.Push(x))
                    {
                        var value = o.GetValueByKey(x);
                        var ex = Properties[x].Validator.Validate(c, value);
                        if (ex != null)
                        {
                            return ex;
                        }
                    }
                }
            }

            return null;
        }

        Dictionary<string, object> m_validValueMap = new Dictionary<string, object>();

        public void Serialize(JsonFormatter f, JsonSchemaValidationContext c, Object o)
        {
            // validate properties
            m_validValueMap.Clear();
            foreach (var kv in Properties)
            {
                var value = o.GetValueByKey(kv.Key);
                var v = kv.Value.Validator;
                using (c.Push(kv.Key))
                {
                    if (v != null && v.Validate(c, value) == null)
                    {
                        m_validValueMap.Add(kv.Key, value);
                    }
                }
            }

            using (f.BeginMapDisposable())
            {
                foreach (var kv in Properties)
                {
                    object value;
                    if (!m_validValueMap.TryGetValue(kv.Key, out value))
                    {
                        continue;
                    }

                    string[] dependencies;
                    if (Dependencies.TryGetValue(kv.Key, out dependencies))
                    {
                        // check dependencies
                        bool hasDependencies = true;
                        foreach (var x in dependencies)
                        {
                            if (!m_validValueMap.ContainsKey(x))
                            {
                                hasDependencies = false;
                                break;
                            }
                        }
                        if (!hasDependencies)
                        {
                            continue;
                        }
                    }

                    // key
                    f.Key(kv.Key);

                    // value
                    using (c.Push(kv.Key))
                    {
                        kv.Value.Validator.Serialize(f, c, value);
                    }
                }
            }
        }

        public void ToJson(JsonFormatter f)
        {
            f.Key("type"); f.Value("object");
            if (Properties.Count > 0)
            {
                f.Key("properties");
                f.BeginMap();
                foreach (var kv in Properties)
                {
                    f.Key(kv.Key);
                    kv.Value.ToJson(f);
                }
                f.EndMap();
            }
        }
    }
}
