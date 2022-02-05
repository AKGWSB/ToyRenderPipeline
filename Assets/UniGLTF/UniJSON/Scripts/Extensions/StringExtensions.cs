namespace UniJSON
{
    public static class StringExtensions
    {
        public static JsonNode ParseAsJson(this string json)
        {
            return JsonParser.Parse(json);
        }
    }
}
