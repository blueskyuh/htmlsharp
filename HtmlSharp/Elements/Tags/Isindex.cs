using System;

namespace HtmlSharp.Elements.Tags
{
    public class IsIndex : Tag
    {
        public string Class { get { return this["class"]; } }

        public string Dir { get { return this["dir"]; } }

        public string Id { get { return this["id"]; } }

        public string Lang { get { return this["lang"]; } }

        public string Prompt { get { return this["prompt"]; } }

        public string Style { get { return this["style"]; } }

        public string Title { get { return this["title"]; } }

        public IsIndex()
            : this(new Element[0])
        {
        }

        public IsIndex(params Element[] children)
            : base(children)
        {
            TagName = "isindex";
        }
    }
}