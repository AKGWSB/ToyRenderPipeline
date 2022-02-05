# UniJSON
JSON serializer and deserializer and schema utilities for Unity(.Net3.5)

## JSON

* https://www.json.org/

## JSON Schema

* http://json-schema.org/
* https://github.com/KhronosGroup/glTF/tree/master/specification/2.0/schema

## JSON Patch

* http://jsonpatch.com/

## ToDo

* [x] anyOf to enum
* [ ] string.pattern
* [x] enum.values
* [x] array.items
* [x] object.required
* [x] object.dependencies
* [ ] object.additionalProperties
* [ ] default value

## Example

```cs
[Serializable]
public class glTFSparseIndices
{
    [JsonSchema(Minimum = 0)]
    public int bufferView;

    [JsonSchema(Minimum = 0)]
    public int byteOffset;

    [JsonSchema(EnumSerializationType = EnumSerializationType.AsInt)]
    public glComponentType componentType;

    // empty schemas
    public object extensions;
    public object extras;
}


[Test]
public void AccessorSparseIndices()
{
    // from JSON schema
    var path = Path.GetFullPath(Application.dataPath + "/../glTF/specification/2.0/schema");
    var SchemaDir = new FileSystemAccessor(path);
    var fromSchema = JsonSchema.ParseFromPath(SchemaDir.Get("accessor.sparse.indices.schema.json"));

    // from C# type definition
    var fromClass = JsonSchema.FromType<glTFSparseIndices>();

    Assert.AreEqual(fromSchema, fromClass);
}
```

