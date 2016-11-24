using System;

namespace SwaggerSharp.Examples
{
    public class ActionVerbAttribute:Attribute
    {
        public string Verb { get; set; } = "post";
    }
}