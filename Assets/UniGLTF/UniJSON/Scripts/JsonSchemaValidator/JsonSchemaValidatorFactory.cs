using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNIJSON_PROFILING
#else
using UnityEngine;
#endif


namespace UniJSON
{
    public static class JsonSchemaValidatorFactory
    {
        struct JsonSchemaItem
        {
            public string Key;
            public JsonSchema Schema;
            public bool Required;
            public string[] Dependencies;
        }

        static IEnumerable<JsonSchemaItem> GetProperties(Type t, PropertyExportFlags exportFlags)
        {
            // fields
            foreach (var fi in t.GetFields())
            {
                var a = fi.GetCustomAttributes(typeof(JsonSchemaAttribute), true).FirstOrDefault() as JsonSchemaAttribute;
                if (a == null)
                {
                    // default
                    if (!fi.IsStatic && fi.IsPublic)
                    {
                        // only public instance field
                        a = new JsonSchemaAttribute();
                    }
                }

                // for array item
                var ia = fi.GetCustomAttributes(typeof(ItemJsonSchemaAttribute), true).FirstOrDefault() as ItemJsonSchemaAttribute;

                if (a == null)
                {
                    //int x = 0;
                }
                else
                {
                    yield return new JsonSchemaItem
                    {
                        Key = fi.Name,
                        Schema = JsonSchema.FromType(fi.FieldType, a, ia),
                        Required = a.Required,
                        Dependencies = a.Dependencies,
                    };
                }
            }

            // properties
            foreach (var pi in t.GetProperties())
            {
                var a = pi.GetCustomAttributes(typeof(JsonSchemaAttribute), true).FirstOrDefault() as JsonSchemaAttribute;

                // for array item
                var ia = pi.GetCustomAttributes(typeof(ItemJsonSchemaAttribute), true).FirstOrDefault() as ItemJsonSchemaAttribute;

                if (a != null)
                {
                    yield return new JsonSchemaItem
                    {
                        Key = pi.Name,
                        Schema = JsonSchema.FromType(pi.PropertyType, a, ia),
                        Required = a.Required,
                        Dependencies = a.Dependencies,
                    };
                }
            }
        }

        public static IJsonSchemaValidator Create(JsonValueType valueType,
            Type t = null,
            BaseJsonSchemaAttribute a = null,
            ItemJsonSchemaAttribute ia = null)
        {
            switch (valueType)
            {
                case JsonValueType.Integer:
                    {
                        var v = new JsonIntValidator();
                        if (a != null)
                        {
                            if (!double.IsNaN(a.Minimum))
                            {
                                v.Minimum = (int)a.Minimum;
                            }
                            if (a.ExclusiveMinimum)
                            {
                                v.ExclusiveMinimum = a.ExclusiveMinimum;
                            }
                            if (!double.IsNaN(a.Maximum))
                            {
                                v.Maximum = (int)a.Maximum;
                            }
                            if (a.ExclusiveMaximum)
                            {
                                v.ExclusiveMaximum = a.ExclusiveMaximum;
                            }
                            if (a.MultipleOf != 0)
                            {
                                v.MultipleOf = (int)a.MultipleOf;
                            }
                        }
                        return v;
                    }

                case JsonValueType.Number:
                    {
                        var v = new JsonNumberValidator();
                        if (a != null)
                        {
                            if (!double.IsNaN(a.Minimum))
                            {
                                v.Minimum = (int)a.Minimum;
                            }
                            if (a.ExclusiveMinimum)
                            {
                                v.ExclusiveMinimum = a.ExclusiveMinimum;
                            }
                            if (!double.IsNaN(a.Maximum))
                            {
                                v.Maximum = (int)a.Maximum;
                            }
                            if (a.ExclusiveMaximum)
                            {
                                v.ExclusiveMaximum = a.ExclusiveMaximum;
                            }
                            if (a.MultipleOf != 0)
                            {
                                v.MultipleOf = (int)a.MultipleOf;
                            }
                        }
                        return v;
                    }

                case JsonValueType.String:
                    {
                        var v = new JsonStringValidator();
                        if (a != null)
                        {
                            if (a.Pattern != null)
                            {
                                v.Pattern = new System.Text.RegularExpressions.Regex(a.Pattern);
                            }
                        }
                        return v;
                    }

                case JsonValueType.Boolean:
                    return new JsonBoolValidator();

                case JsonValueType.Array:
                    {
                        var v = new JsonArrayValidator();
                        if (a != null)
                        {
                            if (a.MinItems != 0)
                            {
                                v.MinItems = a.MinItems;
                            }
                            if (a.MaxItems != 0)
                            {
                                v.MaxItems = a.MaxItems;
                            }

                            if (t != null)
                            {
                                if (ia == null)
                                {
                                    ia = new ItemJsonSchemaAttribute();
                                }

                                Type elementType = null;
                                if (t.IsArray)
                                {
                                    elementType = t.GetElementType();
                                }
                                else if (t.GetIsGenericList())
                                {
                                    elementType = t.GetGenericArguments().First();
                                }

                                if (elementType != null)
                                {
                                    /*
                                    var sub = new JsonSchema
                                    {
                                        SkipComparison = ia.SkipSchemaComparison,
                                        Validator = Create(elementType, ia, null)
                                    };
                                    */
                                    var sub = JsonSchema.FromType(elementType, ia, null);
                                    v.Items = sub;
                                }
                            }
                        }
                        return v;
                    }

                case JsonValueType.Object:
                    {
                        if (t.GetIsGenericDictionary())
                        {
                            var genericFactory = typeof(JsonDictionaryValidator).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
                            var factory = genericFactory.MakeGenericMethod(t.GetGenericArguments()[1]);
                            var v = factory.Invoke(null, null) as IJsonSchemaValidator;
                            return v;
                        }
                        else
                        {
                            var v = new JsonObjectValidator();
                            if (a != null)
                            {
                                if (a.MinProperties > 0)
                                {
                                    v.MinProperties = a.MinProperties;
                                }

                                // props
                                foreach (var prop in GetProperties(t, a.ExportFlags))
                                {
                                    v.Properties.Add(prop.Key, prop.Schema);
                                    if (prop.Required)
                                    {
                                        v.Required.Add(prop.Key);
                                    }
                                    if (prop.Dependencies != null)
                                    {
                                        v.Dependencies.Add(prop.Key, prop.Dependencies);
                                    }
                                }

                            }

                            if (ia != null)
                            {
                                var sub = new JsonSchema
                                {
                                    SkipComparison = ia.SkipSchemaComparison,
                                    Validator = Create(typeof(object), ia, null)
                                };
                                v.AdditionalProperties = sub;
                            }

                            return v;
                        }
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public static IJsonSchemaValidator Create(string t)
        {
            return Create((JsonValueType)Enum.Parse(typeof(JsonValueType), t, true));
        }

        static Dictionary<Type, JsonValueType> s_typeMap = new Dictionary<Type, JsonValueType>
        {
            {typeof(int), JsonValueType.Integer },
            {typeof(float), JsonValueType.Number },
            {typeof(string), JsonValueType.String },
            {typeof(bool), JsonValueType.Boolean },

            // Unity types
            {typeof(Vector3), JsonValueType.Object },
        };

        static JsonValueType ToJsonType(Type t)
        {
            JsonValueType jsonValueType;
            if (s_typeMap.TryGetValue(t, out jsonValueType))
            {
                return jsonValueType;
            }

            if (t.IsArray)
            {
                return JsonValueType.Array;
            }
            if (t.GetIsGenericList())
            {
                return JsonValueType.Array;
            }

            if (t.IsClass)
            {
                return JsonValueType.Object;
            }

            throw new NotImplementedException(string.Format("No JsonType for {0}", t));
        }

        public static IJsonSchemaValidator Create(Type t, BaseJsonSchemaAttribute a, ItemJsonSchemaAttribute ia)
        {
            return Create(ToJsonType(t), t, a, ia);
        }
    }
}
