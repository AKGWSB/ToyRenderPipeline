using NUnit.Framework;
using UnityEngine;


namespace UniJSON
{
    public class JsonFormatterTests
    {
        [Test]
        public void IndentTest()
        {
            var formatter = new JsonFormatter(2);
            formatter.BeginMap();
            formatter.Key("a"); formatter.Value(1);
            formatter.EndMap();

            var json = formatter.ToString();
            Debug.Log(json);
        }
    }
}
