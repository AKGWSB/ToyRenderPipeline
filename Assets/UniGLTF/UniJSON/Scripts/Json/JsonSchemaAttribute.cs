﻿using System;


namespace UniJSON
{
    public enum EnumSerializationType
    {
        AsInt,
        AsLowerString,
    }

    public class BaseJsonSchemaAttribute : Attribute
    {
        #region Annotation
        public string Title;
        public string Description;
        #endregion

        #region integer, number
        public double Minimum = double.NaN;
        public bool ExclusiveMinimum;
        public double Maximum = double.NaN;
        public bool ExclusiveMaximum;
        public double MultipleOf;
        #endregion

        #region string
        public string Pattern;
        #endregion

        #region array
        public int MinItems;
        public int MaxItems;
        #endregion

        #region object
        public JsonValueType ValueType;
        public int MinProperties;
        public bool Required;
        public string[] Dependencies;
        #endregion

        #region enum
        public EnumSerializationType EnumSerializationType;
        public object[] EnumValues;
        public object[] EnumExcludes;
        #endregion

        public PropertyExportFlags ExportFlags = PropertyExportFlags.Default;

        /// <summary>
        /// skip validator comparison
        /// </summary>
        public bool SkipSchemaComparison;

        public void Merge(BaseJsonSchemaAttribute rhs)
        {
            if (rhs == null) return;

            if (string.IsNullOrEmpty(Title))
            {
                Title = rhs.Title;
            }
        }
    }

    public class JsonSchemaAttribute : BaseJsonSchemaAttribute { }

    public class ItemJsonSchemaAttribute : BaseJsonSchemaAttribute { }
}
