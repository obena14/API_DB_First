using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class PrimaryKeyAttribute :Attribute
    {
    }
}
