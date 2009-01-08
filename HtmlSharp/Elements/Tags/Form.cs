using System;

namespace HtmlSharp.Elements.Tags
{
    class Form : Tag, IAllowsNesting
    {
        public Type[] NestingBreakers { get { return new Type[] {  }; } }

        public string Acceptcharset {  get { return this["accept-charset"]; } }

        public string Accept {  get { return this["accept"]; } }

        public string Action {  get { return this["action"]; } }

        public string Class {  get { return this["class"]; } }

        public string Dir {  get { return this["dir"]; } }

        public string Enctype {  get { return this["enctype"]; } }

        public string Id {  get { return this["id"]; } }

        public string Lang {  get { return this["lang"]; } }

        public string Method {  get { return this["method"]; } }

        public string Name {  get { return this["name"]; } }

        public string Onclick {  get { return this["onclick"]; } }

        public string Ondblclick {  get { return this["ondblclick"]; } }

        public string Onkeydown {  get { return this["onkeydown"]; } }

        public string Onkeypress {  get { return this["onkeypress"]; } }

        public string Onkeyup {  get { return this["onkeyup"]; } }

        public string Onmousedown {  get { return this["onmousedown"]; } }

        public string Onmousemove {  get { return this["onmousemove"]; } }

        public string Onmouseout {  get { return this["onmouseout"]; } }

        public string Onmouseover {  get { return this["onmouseover"]; } }

        public string Onmouseup {  get { return this["onmouseup"]; } }

        public string Onreset {  get { return this["onreset"]; } }

        public string Onsubmit {  get { return this["onsubmit"]; } }

        public string Style {  get { return this["style"]; } }

        public string Target {  get { return this["target"]; } }

        public string Title {  get { return this["title"]; } }

        public Form()
        {
            ResetsNesting = true;
            
        }
    }
}