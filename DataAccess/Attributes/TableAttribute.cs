using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute:Attribute
    {
        public string Name { get; set; }
    }
}
