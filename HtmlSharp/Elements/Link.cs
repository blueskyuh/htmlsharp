﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HtmlSharp.Elements
{
    public class Link : Tag
    {
        public Link()
        {
            Name = "link";
            IsSelfClosing = true;
        }
    }
}