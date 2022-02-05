﻿using NUnit.Framework;

namespace UniJSON
{
    public class ValidatorTests
    {
        [Test]
        public void IntValidator()
        {
            var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonIntValidator();
                v.Maximum = 0;
                Assert.NotNull(v.Validate(c, 1));
                Assert.Null(v.Validate(c, 0));
                Assert.Null(v.Validate(c, -1));
            }
            {
                var v = new JsonIntValidator();
                v.Maximum = 0;
                v.ExclusiveMaximum = true;
                Assert.NotNull(v.Validate(c, 1));
                Assert.NotNull(v.Validate(c, 0));
                Assert.Null(v.Validate(c, -1));
            }
            {
                var v = new JsonIntValidator();
                v.Minimum = 0;
                Assert.Null(v.Validate(c, 1));
                Assert.Null(v.Validate(c, 0));
                Assert.NotNull(v.Validate(c, -1));
            }
            {
                var v = new JsonIntValidator();
                v.Minimum = 0;
                v.ExclusiveMinimum = true;
                Assert.Null(v.Validate(c, 1));
                Assert.NotNull(v.Validate(c, 0));
                Assert.NotNull(v.Validate(c, -1));
            }
            {
                var v = new JsonIntValidator();
                v.MultipleOf = 4;
                Assert.Null(v.Validate(c, 4));
                Assert.NotNull(v.Validate(c, 5));
            }
        }

        [Test]
        public void NumberValidator()
        {
            var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonNumberValidator();
                v.Maximum = 0.1;
                Assert.NotNull(v.Validate(c, 1));
                Assert.Null(v.Validate(c, 0.1));
                Assert.Null(v.Validate(c, -1));
            }
            {
                var v = new JsonNumberValidator();
                v.Maximum = 0.1;
                v.ExclusiveMaximum = true;
                Assert.NotNull(v.Validate(c, 1));
                Assert.NotNull(v.Validate(c, 0.1));
                Assert.Null(v.Validate(c, -1));
            }
            {
                var v = new JsonNumberValidator();
                v.Minimum = 0.1;
                Assert.Null(v.Validate(c, 1));
                Assert.Null(v.Validate(c, 0.1));
                Assert.NotNull(v.Validate(c, -1));
            }
            {
                var v = new JsonNumberValidator();
                v.Minimum = 0.1;
                v.ExclusiveMinimum = true;
                Assert.Null(v.Validate(c, 1));
                Assert.NotNull(v.Validate(c, 0.1));
                Assert.NotNull(v.Validate(c, -1));
            }
        }

        [Test]
        public void BoolValidator()
        {
        }

        [Test]
        public void StringValidator()
        {
            var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonStringValidator();
                v.MinLength = 1;
                Assert.Null(v.Validate(c, "a"));
                Assert.NotNull(v.Validate(c, ""));
            }
            {
                var v = new JsonStringValidator();
                v.MaxLength = 1;
                Assert.Null(v.Validate(c, "a"));
                Assert.NotNull(v.Validate(c, "ab"));
            }
            {
                var v = new JsonStringValidator();
                v.Pattern = new System.Text.RegularExpressions.Regex("abc");
                Assert.Null(v.Validate(c, "abc"));
                Assert.NotNull(v.Validate(c, "ab"));
            }
            {
                var v = new JsonStringValidator();
                v.Pattern = new System.Text.RegularExpressions.Regex("ab+");
                Assert.Null(v.Validate(c, "abb"));
                Assert.Null(v.Validate(c, "ab"));
                Assert.NotNull(v.Validate(c, "a"));
            }
        }

        [Test]
        public void StringEnumValidator()
        {
            var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonStringEnumValidator();
                v.Values = new string[] { "a", "b" };
                Assert.Null(v.Validate(c, "a"));
                Assert.NotNull(v.Validate(c, "c"));
            }
        }

        [Test]
        public void IntEnumValidator()
        {
            var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonIntEnumValidator();
                v.Values = new int[] { 1, 2 };
                Assert.Null(v.Validate(c, 1));
                Assert.NotNull(v.Validate(c, 3));
            }
        }

        [Test]
        public void ArrayValidator()
        {
            var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonArrayValidator();
                v.MaxItems = 1;
                Assert.Null(v.Validate(c, new object[] { 0 }));
                Assert.NotNull(v.Validate(c, new object[] { 0, 1 }));
            }
            {
                var v = new JsonArrayValidator();
                v.MinItems = 1;
                Assert.Null(v.Validate(c, new object[] { 0 }));
                Assert.NotNull(v.Validate(c, new object[] { }));
            }
        }

        /// <summary>
        /// ToDo
        /// </summary>
        [Test]
        public void ObjectValidator()
        {
            //var c = new JsonSchemaValidationContext("test");

            {
                var v = new JsonObjectValidator();
                v.MinProperties = 1;
            }
        }
    }
}
